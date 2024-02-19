using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Qos.Apis.QosDiscovery;
using Unity.Services.Qos.QosDiscovery;
using Unity.Services.Qos.Runner;
using UnityEngine;

namespace Unity.Services.Qos
{
    class WrappedQosService : IQosService
    {
        const string ResultLatencyMetricName = "qos_result_latency_ms";
        const string ResultPacketLossMetricName = "qos_result_packet_loss";

        const string MetricServiceNameLabelName = "qos_service_name";
        const string MetricServiceRegionLabelName = "qos_service_region";
        const string MetricClientCountryLabelName = "qos_client_country";
        const string MetricClientRegionLabelName = "qos_client_region";
        const string MetricClientBestResultLabelName = "qos_best_result";
        const string MetricClientBestResultLabelTrueValue = "true";

        IQosDiscoveryApiClient _qosDiscoveryApiClient;

        IQosRunner _qosRunner;

        IAccessToken _accessToken;

        IMetrics _metrics;

        internal WrappedQosService(IQosDiscoveryApiClient qosDiscoveryApiClient, IQosRunner qosRunner,
                                   IAccessToken accessToken, IMetrics metrics)
        {
            _qosDiscoveryApiClient = qosDiscoveryApiClient;
            _qosRunner = qosRunner;
            _accessToken = accessToken;
            _metrics = metrics;
        }

        /// <inheritdoc/>
        /// Implementation of GetSortedQosResultsAsync, where QoS results are sorted by packet latency and _then_ by
        /// packet loss. E.g. Regions for the following pairs of (Latency,PL): (1ms,0%) (1ms,1%) (1ms,3%) (2ms,0%) (2ms,1%) they will
        /// be sorted in the following manner:
        /// Lat P/L
        /// 1ms   0%
        /// 1ms   1%
        /// 1ms   3%
        /// 2ms   0%
        /// 2ms   1%
        ///
        /// Notice that the third entry (1ms,3%) is put before the (2ms,1%) because it has less latency, even if it has more
        /// packet loss.
        /// In case where no QoS servers can be found, no QoS is performed and an empty list is returned.
        public async Task<IList<IQosResult>> GetSortedQosResultsAsync(string service, IList<string> regions)
        {
            return (await GetSortedInternalQosResultsAsync(service, regions))
                .Select(MapToPublicQosResult)
                .ToList();
        }

        internal async Task<IList<Internal.QosResult>> GetSortedInternalQosResultsAsync(string service,
            IList<string> regions)
        {
#if UGS_QOS_SUPPORTED && !UNITY_WEBGL
            if (string.IsNullOrEmpty(_accessToken.AccessToken))
            {
                throw new Exception("Access token not available, please sign in with the Authentication Service.");
            }

            // Code-generated API client requires a (concrete) List but in our interface we only require an IList interface
            // Try to cast IList to List
            var regionsList = regions as List<string>;
            // Else create a List from the IList
            if (regionsList == null && regions != null)
            {
                regionsList = new List<string>(regions);
            }

            var httpResp = await _qosDiscoveryApiClient.GetServersAsync(
                new GetServersRequest(regionsList, service));
            var servers = httpResp.Result.Data.Servers;

            // empty response, return no results
            if (!servers.Any())
            {
                return new List<Internal.QosResult>();
            }

            var qosResults = await _qosRunner.MeasureQosAsync(servers);
            var sortedResults = SortResults(qosResults);

            SendResultsMetrics(sortedResults, service, httpResp);
            return sortedResults;
#else
#if UNITY_WEBGL
            throw new PlatformNotSupportedException(
                "QoS SDK does not support WebGL at this time.");
#else
            throw new UnsupportedEditorVersionException(
                "QoS SDK does not support this version of Unity, please upgrade to 2020.3.34f1+, 2021.3.2f1+, 2022.2.0a10+, or newer.");
#endif
#endif
        }

        List<Internal.QosResult> SortResults(IList<Internal.QosResult> results)
        {
            return results
                .OrderBy(q => q.AverageLatencyMs)
                .ThenBy(q => q.PacketLossPercent)
                .ToList();
        }

        /// <inheritdoc/>
        /// Implementation of GetSortedRelayQosResultsAsync, where QoS results are sorted by packet latency and _then_ by
        /// packet loss. E.g. Regions for the following pairs of (Latency,PL): (1ms,0%) (1ms,1%) (1ms,3%) (2ms,0%) (2ms,1%) they will
        /// be sorted in the following manner:
        /// Lat P/L
        /// 1ms   0%
        /// 1ms   1%
        /// 1ms   3%
        /// 2ms   0%
        /// 2ms   1%
        ///
        /// Notice that the third entry (1ms,3%) is put before the (2ms,1%) because it has less latency, even if it has more
        /// packet loss. Results with 100% packet loss are filtered out and not returned.
        /// In case where no QoS servers can be found, no QoS is performed and an empty list is returned.
        public async Task<IList<IQosAnnotatedResult>> GetSortedRelayQosResultsAsync(IList<string> regions)
        {
            return await GetSortedInternalServiceQosResultsAsync(GetServiceServersRequest.ServiceIdRelay, regions,
                null);
        }

        /// <inheritdoc/>
        /// Implementation of GetSortedMultiplayQosResultsAsync, where QoS results are sorted by packet latency and _then_ by
        /// packet loss. E.g. Regions for the following pairs of (Latency,PL): (1ms,0%) (1ms,1%) (1ms,3%) (2ms,0%) (2ms,1%) they will
        /// be sorted in the following manner:
        /// Lat P/L
        /// 1ms   0%
        /// 1ms   1%
        /// 1ms   3%
        /// 2ms   0%
        /// 2ms   1%
        ///
        /// Notice that the third entry (1ms,3%) is put before the (2ms,1%) because it has less latency, even if it has more
        /// packet loss. Results with 100% packet loss are filtered out and not returned.
        /// In case where no QoS servers can be found, no QoS is performed and an empty list is returned.
        public async Task<IList<IQosAnnotatedResult>> GetSortedMultiplayQosResultsAsync(IList<string> fleet)
        {
            return await GetSortedInternalServiceQosResultsAsync(GetServiceServersRequest.ServiceIdMultiplay, null,
                fleet);
        }

        internal async Task<IList<IQosAnnotatedResult>> GetSortedInternalServiceQosResultsAsync(string service,
            IList<string> regions, IList<string> fleet)
        {
#if UGS_QOS_SUPPORTED && !UNITY_WEBGL
            if (string.IsNullOrEmpty(_accessToken.AccessToken))
            {
                throw new Exception("Access token not available, please sign in with the Authentication Service.");
            }

            // Code-generated API client requires a (concrete) List but in our interface we only require an IList interface
            // Try to cast IList to List
            var regionsList = regions as List<string>;
            // Else create a List from the IList
            if (regionsList == null && regions != null)
            {
                regionsList = new List<string>(regions);
            }

            var fleetList = fleet as List<string>;
            // Else create a List from the IList
            if (fleetList == null && fleet != null)
            {
                fleetList = new List<string>(fleet);
            }

            var httpResp = await _qosDiscoveryApiClient.GetServiceServersAsync(
                new GetServiceServersRequest(service, regionsList, fleetList));
            var servers = httpResp.Result.Data.Servers;

            // empty response, return no results
            if (!servers.Any())
            {
                return new List<IQosAnnotatedResult>();
            }

            var qosResults = await _qosRunner.MeasureQosAsync(servers);
            var sortedResults = SortServiceResults(qosResults);

            SendResultsMetrics(sortedResults.Cast<IQosResult>().ToList(), service, httpResp);
            return sortedResults;
#else
#if UNITY_WEBGL
            throw new PlatformNotSupportedException(
                "QoS SDK does not support WebGL at this time.");
#else
            throw new UnsupportedEditorVersionException(
                "QoS SDK does not support this version of Unity, please upgrade to 2020.3.34f1+, 2021.3.2f1+, 2022.2.0a10+, or newer.");
#endif
#endif
        }

        List<IQosAnnotatedResult> SortServiceResults(IList<QosAnnotatedResult> results)
        {
            // When converting from internal qos results to qos results we use int.MaxValue for invalid latency, so we filter those out.
            // A valid result will have packet loss equals to zero and lower than one. Anything else is considered invalid as either
            // float.MaxValue or 100% of packet loss means we could not get any measures.
            return results
                .Where(q => q.AverageLatencyMs != int.MaxValue && q.PacketLossPercent >= 0.0 &&
                q.PacketLossPercent < 1.0)
                .GroupBy(q => q.Region)
                .Select(q => (IQosAnnotatedResult) new QosResult(q.Key,
                    (int)Math.Round(q.Select(x => x.AverageLatencyMs).Average()),
                    q.Select(x => x.PacketLossPercent).Average(),
                    q.Select(x => x.Annotations).First()))
                .ToList()
                .OrderBy(q => q.AverageLatencyMs)
                .ThenBy(q => q.PacketLossPercent)
                .ToList();
        }

        void SendResultsMetrics(IList<Internal.QosResult> sortedResults, string service, Response discoveryResponse)
        {
            // don't send metrics in the rare case that there are no results
            if (sortedResults.Count == 0)
            {
                return;
            }

            for (var index = 0; index < sortedResults.Count; index++)
            {
                var result = sortedResults[index];
                SendResultMetrics(service, discoveryResponse, result.Region, result.AverageLatencyMs,
                    result.PacketLossPercent, index == 0);
            }
        }

        void SendResultsMetrics(IList<IQosResult> sortedResults, string service, Response discoveryResponse)
        {
            // don't send metrics in the rare case that there are no results
            if (sortedResults.Count == 0)
            {
                return;
            }

            for (var index = 0; index < sortedResults.Count; index++)
            {
                var result = sortedResults[index];
                SendResultMetrics(service, discoveryResponse, result.Region, result.AverageLatencyMs,
                    result.PacketLossPercent, index == 0);
            }
        }

        void SendResultMetrics(string service, Response discoveryResponse, string region, int averageLatencyMs,
            float packetLossPercent, bool isBest)
        {
            IDictionary<string, string> metricTags = new Dictionary<string, string>();

            metricTags.Add(MetricServiceNameLabelName, service);
            metricTags.Add(MetricServiceRegionLabelName, region);
            if (discoveryResponse.Headers.TryGetValue("X-Client-Country", out var clientCountry))
            {
                metricTags.Add(MetricClientCountryLabelName, clientCountry);
            }

            if (discoveryResponse.Headers.TryGetValue("X-Client-Region", out var clientRegion))
            {
                metricTags.Add(MetricClientRegionLabelName, clientRegion);
            }

            // only add the "best_result" tag when processing the first (best) result
            if (isBest)
            {
                metricTags.Add(MetricClientBestResultLabelName, MetricClientBestResultLabelTrueValue);
            }

            _metrics.SendHistogramMetric(ResultLatencyMetricName, averageLatencyMs, metricTags);
            _metrics.SendHistogramMetric(ResultPacketLossMetricName, packetLossPercent, metricTags);
        }

        IQosResult MapToPublicQosResult(Internal.QosResult internalQosResult)
        {
            return new QosResult(internalQosResult.Region, internalQosResult.AverageLatencyMs,
                internalQosResult.PacketLossPercent);
        }
    }

    class QosResult : IQosAnnotatedResult
    {
        public QosResult(string region, int averageLatencyMs, float packetLossPercent,
                         Dictionary<string, List<string>> annotations = default)
        {
            Region = region;
            AverageLatencyMs = averageLatencyMs;
            PacketLossPercent = packetLossPercent;
            Annotations = annotations ?? new Dictionary<string, List<string>>();
        }

        public string Region { get; }
        public int AverageLatencyMs { get; }
        public float PacketLossPercent { get; }
        public Dictionary<string, List<string>> Annotations { get; }
    }

    class UnsupportedEditorVersionException : Exception
    {
        public UnsupportedEditorVersionException()
        {
        }

        public UnsupportedEditorVersionException(string message) : base(message)
        {
        }
    }
}
