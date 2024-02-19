using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
#if UGS_QOS_SUPPORTED
using System.Text;
using Unity.Baselib.LowLevel;
using UnityEngine;
using Random = System.Random;
#endif
using Unity.Services.Qos.Runner;

namespace Unity.Networking.QoS
{
    partial struct QosJob : IQosJob
    {
#if UGS_QOS_SUPPORTED
        uint RequestsPerEndpoint;
        ulong TimeoutMs;
        ulong MaxWaitMs;
        uint RequestsBetweenPause;
        uint RequestPauseMs;
        uint ReceiveWaitMs;

        // Leave the results allocated after the job is done. It's the user's responsibility to dispose it.
        NativeArray<InternalQosResult> _qosResults;

        public JobHandle Schedule<T>(JobHandle dependsOn = default) where T : struct, IJob
        {
            return this.Schedule(dependsOn);
        }

        public NativeArray<InternalQosResult> QosResults => _qosResults;

        [DeallocateOnJobCompletion] NativeArray<InternalQosServer> m_QosServers;
        [DeallocateOnJobCompletion] NativeArray<byte> m_TitleBytesUtf8;
        NativeHashMap<FixedString64Bytes, int> m_AddressIndexes;
        DateTime m_JobExpireTimeUtc;
        int m_Requests;
        int m_Responses;

        internal QosJob(IList<UcgQosServer> qosServers, string title,
                        uint requestsPerEndpoint = 5, ulong timeoutMs = 10000, ulong maxWaitMs = 500,
                        uint requestsBetweenPause = 10, uint requestPauseMs = 1, uint receiveWaitMs = 10) : this()
        {
            RequestsPerEndpoint = requestsPerEndpoint;
            TimeoutMs = timeoutMs;
            MaxWaitMs = maxWaitMs;
            RequestsBetweenPause = requestsBetweenPause;
            RequestPauseMs = requestPauseMs;
            ReceiveWaitMs = receiveWaitMs;

            // Copy the QoS Servers into the job, converting all the IP/Port to NetworkEndPoint and DateTime to ticks.
            m_AddressIndexes = new NativeHashMap<FixedString64Bytes, int>(qosServers?.Count ?? 0, Allocator.Persistent);
            m_QosServers = new NativeArray<InternalQosServer>(qosServers?.Count ?? 0, Allocator.Persistent);
            if (qosServers != null)
            {
                var i = 0;
                foreach (var s in qosServers)
                {
                    if (!NetworkEndPoint.TryParse(s.ipv4, s.port, out var remote))
                    {
                        Debug.LogError($"QosJob: Invalid IP address {s.ipv4} in QoS Servers list");
                        continue;
                    }

                    var server = new InternalQosServer(remote, s.BackoffUntilUtc, i);

                    // check if the server already exists
                    if (m_AddressIndexes.ContainsKey(server.Address))
                    {
                        // Duplicate server.
                        server.FirstIdx = m_AddressIndexes[server.Address];
                    }
                    else
                    {
                        m_AddressIndexes.Add(server.Address, i);
                    }

                    StoreServer(server);
                    i++;
                }

                if (i < m_QosServers.Length)
                {
                    // We had some bad addresses, resize m_QosServers to reduce storage but more importantly
                    // so iterations can be used without checking. m_AddressIndexes isn't resized as its not
                    // worth the effort for the small amount of memory which would saved.
                    NativeArray<InternalQosServer> t = new NativeArray<InternalQosServer>(i, Allocator.Persistent);
                    m_QosServers.GetSubArray(0, t.Length).CopyTo(t);
                    m_QosServers.Dispose();
                    m_QosServers = t;
                }
            }

            // Indexes into QosResults correspond to indexes into qosServers/m_QosServers
            _qosResults = new NativeArray<InternalQosResult>(m_QosServers.Length, Allocator.Persistent);

            // Convert the title to a NativeArray of bytes (since everything in the job has to be a value-type)
            byte[] utf8Title = Encoding.UTF8.GetBytes(title);
            m_TitleBytesUtf8 = new NativeArray<byte>(utf8Title.Length, Allocator.Persistent);
            m_TitleBytesUtf8.CopyFrom(utf8Title);
        }

        /// <summary>
        /// Disposes of any internal structures.
        /// </summary>
        public void Dispose()
        {
            if (m_AddressIndexes.IsCreated)
                m_AddressIndexes.Dispose();
        }

        /// <summary>
        // Execute implements IJob
        /// </summary>
        public void Execute()
        {
            if (m_QosServers.Length == 0)
                return;    // Nothing to do.

            m_Requests = 0;
            m_Responses = 0;
            var startTime = DateTime.UtcNow;
            m_JobExpireTimeUtc = startTime.AddMilliseconds(TimeoutMs);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"QosJob: executing job with {TimeoutMs}ms timeout");
#endif

            // Create the local socket
            Binding.Baselib_ErrorCode errorCode;
            Binding.Baselib_Socket_Handle socket;
            (socket, errorCode) = CreateAndBindSocket();

            if (errorCode != Binding.Baselib_ErrorCode.Success)
            {
                // Can't run the job
                Debug.LogError($"QosJob: failed to create and bind the local socket (errorcode {errorCode})");
                return;
            }

            ProcessServers(socket);

            Binding.Baselib_Socket_Close(socket);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"QosJob: took {QosHelper.Since(startTime)} to process {m_QosServers.Length} servers");
#endif
        }

        /// <summary>
        /// Sends QoS requests to all servers and receives the responses.
        /// </summary>
        void ProcessServers(Binding.Baselib_Socket_Handle socketHandle)
        {
            // Create placeholder endpoint
            var endpoint = default(NetworkEndPoint);
            foreach (var s in m_QosServers)
            {
                if (s.Duplicate)
                {
                    // Skip, we will copy the result before we return.
                    continue;
                }

                ProcessServer(s, socketHandle);

                // To get as accurate results as possible check to see if we have pending responses.
                RecvQosResponsesTimed(endpoint, m_JobExpireTimeUtc, socketHandle, false);
            }

            // Receive remaining responses.
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(MaxWaitMs);
            if (m_JobExpireTimeUtc < deadline)
            {
                deadline = m_JobExpireTimeUtc;
            }

            var error = EnableReceiveWait();
            if (error != "")
            {
                Debug.LogError(error);
                return;
            }
            RecvQosResponsesTimed(endpoint, deadline, socketHandle, true);

            foreach (var s in m_QosServers)
            {
                // If duplicate server, just copy the result.
                var r = s.Duplicate ? _qosResults[s.FirstIdx] : _qosResults[s.Idx];
                StoreResult(s.Idx, r);
            }
        }

        /// <summary>
        /// Sends QoS requests to a server and receives any responses that are ready.
        /// </summary>
        /// <param name="server">Server that QoS requests should be sent to</param>
        /// <param name="socketHandle">The socket handle to use to send/receive packets</param>
        void ProcessServer(InternalQosServer server, Binding.Baselib_Socket_Handle socketHandle)
        {
            if (QosHelper.ExpiredUtc(m_JobExpireTimeUtc))
            {
                Debug.LogWarning($"QosJob: not enough time to process {server.Address}.");
                return;
            }

            if (DateTime.UtcNow < server.BackoffUntilUtc)
            {
                Debug.LogWarning($"QosJob: skipping {server.Address} due to backoff restrictions");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DateTime startTime = DateTime.UtcNow;
#endif

            InternalQosResult r = _qosResults[server.Idx];
            var errorcode = SendQosRequests(server, socketHandle, ref r);
            if (errorcode != 0)
            {
                Debug.LogError($"QosJob: failed to send to {server.Address} (errorcode {errorcode})");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"QosJob: send to {server.Address} took {QosHelper.Since(startTime)}");
#endif
            StoreResult(server.Idx, r);
        }

        /// <summary>
        /// Sends QoS requests to the given server.
        /// </summary>
        /// <param name="server">Server that QoS requests should be sent to</param>
        /// <param name="socketHandle">The baselib socket handle to use for sending the requests</param>
        /// <param name="result">Results from the send side of the check (packets sent)</param>
        /// <returns>
        /// errorcode - the last error code generated (if any).  0 indicates success.
        /// </returns>
        Binding.Baselib_ErrorCode SendQosRequests(InternalQosServer server, Binding.Baselib_Socket_Handle socketHandle, ref InternalQosResult result)
        {
            QosRequest request = new QosRequest
            {
                Title = m_TitleBytesUtf8.ToArray(),
                Identifier = (ushort)new Random().Next(ushort.MinValue, ushort.MaxValue),
            };
            server.RequestIdentifier = request.Identifier;
            StoreServer(server);

            // Send all requests.
            result.RequestsSent = 0;
            do
            {
                if (QosHelper.ExpiredUtc(m_JobExpireTimeUtc))
                {
                    Debug.LogWarning($"QosJob: not enough time to complete {RequestsPerEndpoint - result.RequestsSent} sends to {server.Address} ");
                    return Binding.Baselib_ErrorCode.Timeout;
                }
                request.Timestamp = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
                request.Sequence = (byte)result.RequestsSent;

                int errorCode;
                uint sent;
                (sent, errorCode) = request.Send(socketHandle.handle, server.RemoteEndpoint, m_JobExpireTimeUtc);
                if ((Binding.Baselib_ErrorCode)errorCode != Binding.Baselib_ErrorCode.Success)
                {
                    Debug.LogError($"QosJob: send returned error code {(Binding.Baselib_ErrorCode)errorCode}, can't continue");
                    return (Binding.Baselib_ErrorCode)errorCode;
                }
                else if (sent != request.Length)
                {
                    Debug.LogWarning($"QosJob: sent {sent} of {request.Length} bytes, ignoring this request");
                    result.InvalidRequests++;
                }
                else
                {
                    m_Requests++;
                    result.RequestsSent++;
                    if (RequestsBetweenPause > 0 && RequestPauseMs > 0 && m_Requests % RequestsBetweenPause == 0)
                    {
                        // Inject a delay to help avoid network overload and to space requests out over time
                        // which can improve the accuracy of the results.
                        System.Threading.Thread.Sleep((int)RequestPauseMs);
                    }
                }
            }
            while (result.RequestsSent < RequestsPerEndpoint);

            return 0;
        }

        /// <summary>
        /// Stores the updated server.
        /// </summary>
        /// <param name="server">The server to store</param>
        void StoreServer(InternalQosServer server)
        {
            m_QosServers[server.Idx] = server;
        }

        /// <summary>
        /// Stores the updated result.
        /// </summary>
        /// <param name="idx">The index to store the result at</param>
        /// <param name="result">The result to store</param>
        void StoreResult(int idx, InternalQosResult result)
        {
            _qosResults[idx] = result;
        }

        /// <summary>
        /// Receive QoS responses outputing timing information to the log if in the editor or development mode.
        /// </summary>
        /// <param name="addr">The interface address for storage</param>
        /// <param name="deadline">Responses after this point in time may not be processed</param>
        /// <param name="wait">If true waits for all pending responses to be received, otherwise returns early if no response is received</param>
        void RecvQosResponsesTimed(NetworkEndPoint addr, DateTime deadline, Binding.Baselib_Socket_Handle socketHandle, bool wait)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int hadResponses = m_Responses;
            DateTime startTime = DateTime.UtcNow;
#endif
            RecvQosResponses(addr, deadline, socketHandle, wait);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var t = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var p = m_Responses - hadResponses;
            string avgTime = "";
            if (p > 0)
            {
                avgTime = $" avg {t/p:F0}ms per response";
                string w = wait ? "waiting" : "";
                Debug.Log($"QosJob: received {p} responses of {m_Responses}/{m_Requests} in {QosHelper.Since(startTime)} {w}{avgTime}");
            }
#endif
        }

        /// <summary>
        /// Receive QoS responses
        /// </summary>
        /// <param name="addr">The interface address for storage</param>
        /// <param name="deadline">Responses after this point in time may not be processed</param>
        /// <param name="socketHandle">A Baselib socket handle to use for receiving responses</param>
        /// <param name="wait">If true waits for all pending responses to be received, otherwise returns as soon as no more responses are received</param>
        void RecvQosResponses(NetworkEndPoint addr, DateTime deadline, Binding.Baselib_Socket_Handle socketHandle, bool wait)
        {
            if (m_Requests == m_Responses)
                return;

            QosResponse response = new QosResponse();
            InternalQosResult result = _qosResults[0];

            while (m_Requests > m_Responses)
            {
                if (QosHelper.ExpiredUtc(deadline))
                {
                    // Even though this could indicate a config issue the most common cause
                    // will be packet loss so use debug not warning.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"QosJob: not enough time to receive {m_Requests - m_Responses} outstanding responses");
#endif
                    return;
                }

                int received;
                (received, _) = response.Recv(socketHandle.handle, wait, deadline, ref addr);
                if (received == 0)
                {
                    if (!wait)
                        return; // Wait disabled so just return.

                    continue; // Timeout so just retry.
                }

                if (received == -1)
                {
                    // Error was logged by Recv so just retry.
                    continue;
                }
                int idx = LookupResult(addr, response, ref result);
                if (idx < 0)
                {
                    // Not found.
                    continue;
                }

                string error = "";
                if (!response.Verify(result.RequestsSent, ref error))
                {
                    Debug.LogWarning($"QosJob: ignoring response from {m_QosServers[idx].Address} verify failed with {error}");
                    result.InvalidResponses++;
                }
                else
                {
                    m_Responses++;
                    result.ResponsesReceived++;
                    result.AddAggregateLatency((uint)response.LatencyMs);

                    // Determine if we've had flow control applied to us. If so, save the most significant result based
                    // on the unit count. In this version, both Ban and Throttle have the same result: client back-off.
                    var fc = response.ParseFlowControl();
                    if (fc.type != FcType.None && fc.units > result.FcUnits)
                    {
                        result.FcType = fc.type;
                        result.FcUnits = fc.units;
                    }
                }

                StoreResult(idx, result);
            }
        }

        /// <summary>
        /// Enables receive waiting and disables non blocking socket operations.
        /// This reduces the overhead of receiving pending responses after all sends have completed.
        /// </summary>
        /// <returns>
        /// error - string detailing on failure, otherwise and empty string.
        /// </returns>
        string EnableReceiveWait()
        {
            // TODO: implement blocking on baselib
            return "";
        }

        /// <summary>
        /// Returns the index of the server which matches both the address of endPoint and identified of response.
        /// </summary>
        /// <returns>
        /// index - the index of server if found, otherwise -1.
        /// </returns>
        int LookupResult(NetworkEndPoint endPoint, QosResponse response, ref InternalQosResult result)
        {
            // TODO(steve): Connecting to loopback at nonstandard (but technically correct) addresses like
            // 127.0.0.2 can return a remote address of 127.0.0.1, resulting in a mismatch which we could fix.
            if (m_AddressIndexes.TryGetValue(endPoint.Address, out var idx))
            {
                result = _qosResults[idx];
                var s = m_QosServers[idx];
                if (response.Identifier != s.RequestIdentifier)
                {
                    Debug.LogWarning($"QosJob: invalid identifier from {s.Address} 0x{response.Identifier:X4} != 0x{s.RequestIdentifier:X4} ignoring");
                    result.InvalidResponses++;
                    return -1;
                }

                return idx;
            }

            Debug.LogWarning($"QosJob: ignoring unexpected response from {endPoint.Address}");

            return -1;
        }

        /// <summary>
        /// Create and bind the local UDP socket for QoS checks. Also sets appropriate options on the socket such as
        /// non-blocking and buffer sizes.
        /// </summary>
        /// <returns>
        /// (socketfd, errorcode) where socketfd is a native socket descriptor and errorcode is the error code (if any)
        /// errorcode is 0 on no error.
        /// </returns>
        static unsafe (Binding.Baselib_Socket_Handle, Binding.Baselib_ErrorCode) CreateAndBindSocket()
        {
            var errorState = default(Binding.Baselib_ErrorState);

            // Create the socket handle.
            var socketHandle = Binding.Baselib_Socket_Create(Binding.Baselib_NetworkAddress_Family.IPv4,
                Binding.Baselib_Socket_Protocol.UDP, &errorState);

            if (errorState.code != Binding.Baselib_ErrorCode.Success)
            {
                Debug.LogError($"QosJob: Unable to create socket {errorState.code}");
            }

            return (socketHandle, errorState.code);
        }
    }
#else
        internal QosJob(IList<UcgQosServer> qosServers, string title) : this()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            throw new NotImplementedException();
        }

        public JobHandle Schedule<T>(JobHandle dependsOn = default) where T : struct, IJob
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public NativeArray<InternalQosResult> QosResults { get; }
    }
#endif
}
