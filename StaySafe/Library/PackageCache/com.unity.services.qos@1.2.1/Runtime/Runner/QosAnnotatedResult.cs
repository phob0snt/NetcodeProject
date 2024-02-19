using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Qos.Internal;
using Unity.Services.Qos.Models;

namespace Unity.Services.Qos.Runner
{
    /// <summary>
    /// Represents the results of QoS measurements for a given region.
    /// </summary>
    public struct QosAnnotatedResult
    {
        /// <summary>
        /// The identifier for the service's region used in this set of QoS measurements.
        /// </summary>
        /// <value>A string containing the region name.
        /// </value>
        public string Region;
        /// <summary>
        /// Average latency of QoS measurements to the region.
        /// </summary>
        /// <remarks>
        /// The latency is determined by measuring the time between sending a packet and receiving the response for that packet,
        /// then taking the average for all responses received. Only packets for which a response was received are
        /// considered in the calculation.
        /// </remarks>
        /// <value>A positive integer, in milliseconds.</value>
        public int AverageLatencyMs;
        /// <summary>
        /// Percentage of packet loss observed in QoS measurements to the region.
        /// </summary>
        /// <remarks>
        /// Packet loss is determined by counting the number of packets for which a response was received from the QoS server,
        /// then taking the percentage based on the total number of packets sent.
        /// </remarks>
        /// <value>A positive flow value. The range is 0.0f - 1.0f (0 - 100%).</value>
        public float PacketLossPercent;
        /// <summary>
        /// The results annotations.
        /// </summary>
        /// <value>A dictionary of additional information.
        /// </value>
        public Dictionary<string, List<string>> Annotations;
    }
}
