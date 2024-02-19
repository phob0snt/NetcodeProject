using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Networking.QoS;
using Unity.Services.Qos.Models;
using Debug = UnityEngine.Debug;

namespace Unity.Services.Qos.Runner
{
    delegate IQosJob QosJobProvider(IList<UcgQosServer> servers, string title);

    delegate Task<IPAddress[]> DnsResolver(string host);

    class BaselibQosRunner : IQosRunner
    {
        QosJobProvider _qosJobProvider = (servers, title) => new QosJob(servers, title);
        DnsResolver _dnsResolver = Dns.GetHostAddressesAsync;

        public BaselibQosRunner(QosJobProvider qosJobProvider = null, DnsResolver dnsResolver = null)
        {
            if (qosJobProvider != null)
            {
                _qosJobProvider = qosJobProvider;
            }

            if (dnsResolver != null)
            {
                _dnsResolver = dnsResolver;
            }
        }

        // MeasureQos will return an empty list if an error occurs
        public async Task<List<Internal.QosResult>> MeasureQosAsync(IList<QosServer> servers)
        {
            var convertedServers = (await Task.WhenAll(servers.Select(ToUcgFormat)))
                .Where(s => s.HasValue)
                .Select(s => s.Value)
                .ToList();

            var results = new List<Internal.QosResult>();
            var job = await RunQosJob(convertedServers);
            if (servers.Count() == job.QosResults.Count())
            {
                results = ParseResults(job.QosResults, servers);
            }

            job.Dispose();
            job.QosResults.Dispose();
            return results;
        }

        public async Task<List<QosAnnotatedResult>> MeasureQosAsync(IList<QosServiceServer> servers)
        {
            var convertedServers = (await Task.WhenAll(servers.Select(ToUcgFormat)))
                .Where(s => s.HasValue)
                .Select(s => s.Value)
                .ToList();

            var results = new List<QosAnnotatedResult>();
            var job = await RunQosJob(convertedServers);
            if (servers.Count() == job.QosResults.Count())
            {
                results = ParseResults(job.QosResults, servers);
            }

            job.Dispose();
            job.QosResults.Dispose();
            return results;
        }

        async Task<IQosJob> RunQosJob(List<UcgQosServer> convertedServers)
        {
            var title = "QoS request";
            var job = _qosJobProvider(convertedServers, title);

            var handle = job.Schedule<QosJob>();
            while (!handle.IsCompleted)
            {
                await Task.Yield();
            }

            handle.Complete();

            return job;
        }

        // Helper method to covert a Discovery service QoS server model to the UCG QoS package server model.
        // the main difference is the 'address' and 'port' fields vs a 'endpoint' field.
        // If the 'endpoint' field cannot be parsed, this will return a null struct.
        async Task<UcgQosServer?> ToUcgFormat(QosServer server)
        {
            var serverEndpoint = server.Endpoints[0];
            var serverRegion = server.Region;
            return await ToUcgFormat(serverEndpoint, serverRegion);
        }

        async Task<UcgQosServer?> ToUcgFormat(QosServiceServer server)
        {
            var serverEndpoint = server.Endpoints[0];
            var serverRegion = server.Region;
            return await ToUcgFormat(serverEndpoint, serverRegion);
        }

        async Task<UcgQosServer?> ToUcgFormat(string serverEndpoint, string serverRegion)
        {
            if (!Uri.TryCreate($"udp://{serverEndpoint}", UriKind.Absolute, out var uri))
            {
                Debug.LogError($"Could not create address from endpoint: '{serverEndpoint}'");
                return null;
            }

            if (uri.Port == -1)
            {
                Debug.LogError($"Missing or invalid port in endpoint: '{serverEndpoint}'");
                return null;
            }

            // If uri.Host is an IP, Dns.GetHostAddressesAsync will just use the address instead of attempting to resolve
            var resolvedIps = await _dnsResolver(uri.Host);
            if (resolvedIps.Length == 0)
            {
                Debug.LogError($"No addresses could be resolved for host {uri.Host}");
                return null;
            }

            // Choose first IP from list
            // TODO: we could eventually provide ability to select IPv4/IPv6 specifically
            var ip = resolvedIps[0];

            var ucgServer = new UcgQosServer
            {
                regionid = serverRegion, ipv6 = null, port = Convert.ToUInt16(uri.Port), BackoffUntilUtc = default
            };

            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                ucgServer.ipv4 = ip.ToString();
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
                ucgServer.ipv6 = ip.ToString();
            }

            return ucgServer;
        }

        static List<Internal.QosResult> ParseResults(IEnumerable<InternalQosResult> ucgResults,
            IEnumerable<QosServer> servers)
        {
            var results = new List<Internal.QosResult>();
            using var enr = servers.GetEnumerator();
            foreach (InternalQosResult r in ucgResults)
            {
                enr.MoveNext();
                if (enr.Current == null)
                {
                    break;
                }

                // avoid overflow when converting from uint to int
                int avgLatency = r.AverageLatencyMs > int.MaxValue ? int.MaxValue : (int)r.AverageLatencyMs;

                results.Add(new Internal.QosResult
                {
                    Region =
                        enr.Current
                            .Region, // note: the results from ucg do not contain the region, so we assume they're in the same order as the servers list to populate the field.
                    AverageLatencyMs = avgLatency,
                    PacketLossPercent = r.PacketLoss
                });
            }

            return results;
        }

        static List<QosAnnotatedResult> ParseResults(IEnumerable<InternalQosResult> ucgResults,
            IEnumerable<QosServiceServer> servers)
        {
            var results = new List<QosAnnotatedResult>();
            using var enr = servers.GetEnumerator();
            foreach (InternalQosResult r in ucgResults)
            {
                enr.MoveNext();
                if (enr.Current == null)
                {
                    break;
                }

                // avoid overflow when converting from uint to int
                int avgLatency = r.AverageLatencyMs > int.MaxValue ? int.MaxValue : (int)r.AverageLatencyMs;

                results.Add(new QosAnnotatedResult
                {
                    Region =
                        enr.Current
                            .Region, // note: the results from ucg do not contain the region, so we assume they're in the same order as the servers list to populate the field.
                    AverageLatencyMs = avgLatency,
                    PacketLossPercent = r.PacketLoss,
                    Annotations = enr.Current.Annotations
                });
            }

            return results;
        }
    }
}
