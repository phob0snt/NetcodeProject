using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Relay.Http;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Unity.Services.Relay
{
    /// <summary>
    /// The Allocations Service enables clients to connect to relay servers. Once connected, they are able to communicate with each other, via the relay servers, using the bespoke relay binary protocol.
    /// </summary>
#pragma warning disable CS0618 // IRelayServiceSDK is obsolete, but we still want to implement it for backwards compatibility for now
    internal class WrappedRelayService : IRelayService, IRelayServiceSDK, IRelayServiceSDKConfiguration
#pragma warning restore CS0618
    {
        internal IRelayServiceSdk m_RelayService { get; set; }

        private const string QosRelayServiceName = "relay";

        internal WrappedRelayService(IRelayServiceSdk relayService)
        {
            m_RelayService = relayService;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Thrown when the maxConnections argument fails validation in SDK.</exception>
        public async Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
        {
            EnsureSignedIn();

            if (maxConnections <= 0)
            {
                throw new ArgumentException("Maximum number of connections for an allocation must be greater than 0!");
            }

            if (m_RelayService.QosResults == null)
            {
                throw new Exception("Qos component should not be null, check that is is properly initialized.");
            }
            if (string.IsNullOrEmpty(region))
            {
                try
                {
                    var regions = (await ListRegionsAsync()).Select(r => r.Id).ToList();
                    var qosResults =
                        await m_RelayService.QosResults.GetSortedQosResultsAsync(QosRelayServiceName, regions);
                    // pick first region in the sorted list (best latency + packet loss)
                    if (qosResults.Any())
                    {
                        region = qosResults[0].Region;
                        Debug.Log($"best region is {region}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not do Qos region selection. Will use default.{Environment.NewLine}" +
                        $"QoS failed due to [{ex.GetType().Name}]. Reason: {ex.Message}");
                }
            }

            try
            {
                var response = await m_RelayService.AllocationsApi.CreateAllocationAsync(
                    new RelayAllocations.CreateAllocationRequest(new AllocationRequest(maxConnections, region)),
                    m_RelayService.Configuration);
                return response.Result.Data.Allocation;
            }
            catch (HttpException<ErrorResponseBody> e)
            {
                throw new RelayServiceException(e.ActualError.GetExceptionReason(), e.ActualError.GetExceptionMessage(),
                    e);
            }
            catch (HttpException e)
            {
                if (e.Response.IsHttpError)
                {
                    throw new RelayServiceException(e.Response.GetExceptionReason(), e.Response.ErrorMessage, e);
                }

                if (e.Response.IsNetworkError)
                {
                    throw new RelayServiceException(RelayExceptionReason.NetworkError, e.Response.ErrorMessage);
                }

                throw new RequestFailedException((int) RelayExceptionReason.Unknown, "Something went wrong.", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an errorr.</exception>
        /// <exception cref="RequestFailedException">Thrown when the request does not reach the Relay Allocation Service.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the allocationId argument fails validation in SDK.</exception>
        public async Task<string> GetJoinCodeAsync(Guid allocationId)
        {
            EnsureSignedIn();

            if (allocationId == null || allocationId == Guid.Empty)
            {
                throw new ArgumentNullException("AllocationId cannot be null or empty!");
            }

            try
            {
                var response = await m_RelayService.AllocationsApi.CreateJoincodeAsync(
                    new RelayAllocations.CreateJoincodeRequest(new JoinCodeRequest(allocationId)),
                    m_RelayService.Configuration);
                return response.Result.Data.JoinCode;
            }
            catch (HttpException<ErrorResponseBody> e)
            {
                throw new RelayServiceException(e.ActualError.GetExceptionReason(), e.ActualError.GetExceptionMessage(),
                    e);
            }
            catch (HttpException e)
            {
                if (e.Response.IsHttpError)
                {
                    throw new RelayServiceException(e.Response.GetExceptionReason(), e.Response.ErrorMessage, e);
                }

                if (e.Response.IsNetworkError)
                {
                    throw new RelayServiceException(RelayExceptionReason.NetworkError, e.Response.ErrorMessage);
                }

                throw new RequestFailedException((int) RelayExceptionReason.Unknown, "Something went wrong.", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an error.</exception>
        /// <exception cref="RequestFailedException">Thrown when the request does not reach the Relay Allocation Service.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the joinCode argument fails validation in SDK.</exception>
        public async Task<JoinAllocation> JoinAllocationAsync(string joinCode)
        {
            EnsureSignedIn();

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                throw new ArgumentNullException(
                    "JoinCode must be non-null, non-empty, and cannot contain only whitespace!");
            }

            try
            {
                var response = await m_RelayService.AllocationsApi.JoinRelayAsync(
                    new RelayAllocations.JoinRelayRequest(new JoinRequest(joinCode)), m_RelayService.Configuration);

                return response.Result.Data.Allocation;
            }
            catch (HttpException<ErrorResponseBody> e)
            {
                throw new RelayServiceException(e.ActualError.GetExceptionReason(), e.ActualError.GetExceptionMessage(),
                    e);
            }
            catch (HttpException e)
            {
                if (e.Response.IsHttpError)
                {
                    throw new RelayServiceException(e.Response.GetExceptionReason(), e.Response.ErrorMessage, e);
                }

                if (e.Response.IsNetworkError)
                {
                    throw new RelayServiceException(RelayExceptionReason.NetworkError, e.Response.ErrorMessage);
                }

                throw new RequestFailedException((int) RelayExceptionReason.Unknown, "Something went wrong.", e);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="RelayServiceException">Thrown when the request successfully reach the Relay Allocation Service but results in an error.</exception>
        /// <exception cref="RequestFailedException">Thrown when the request does not reach the Relay Allocation Service.</exception>
        public async Task<List<Region>> ListRegionsAsync()
        {
            EnsureSignedIn();

            try
            {
                var response =
                    await m_RelayService.AllocationsApi.ListRegionsAsync(new RelayAllocations.ListRegionsRequest(),
                        m_RelayService.Configuration);

                return response.Result.Data.Regions;
            }
            catch (HttpException<ErrorResponseBody> e)
            {
                throw new RelayServiceException(e.ActualError.GetExceptionReason(), e.ActualError.GetExceptionMessage(),
                    e);
            }
            catch (HttpException e)
            {
                if (e.Response.IsHttpError)
                {
                    throw new RelayServiceException(e.Response.GetExceptionReason(), e.Response.ErrorMessage, e);
                }

                if (e.Response.IsNetworkError)
                {
                    throw new RelayServiceException(RelayExceptionReason.NetworkError, e.Response.ErrorMessage);
                }

                throw new RequestFailedException((int) RelayExceptionReason.Unknown, "Something went wrong.", e);
            }
        }

        public void SetAllocationsServiceBasePath(string allocationsBasePath)
        {
            this.m_RelayService.Configuration.BasePath = allocationsBasePath;
        }

        private void EnsureSignedIn()
        {
            if (m_RelayService.AccessToken.AccessToken == null)
            {
                throw new RelayServiceException(RelayExceptionReason.Unauthorized,
                    "You are not signed in to the Authentication Service. Please sign in.");
            }
        }
    }
}