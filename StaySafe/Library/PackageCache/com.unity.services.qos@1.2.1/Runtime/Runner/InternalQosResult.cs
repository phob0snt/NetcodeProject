namespace Unity.Networking.QoS
{
    internal struct InternalQosResult
    {
        internal const uint InvalidLatencyValue = uint.MaxValue;
        internal const float InvalidPacketLossValue = float.MaxValue;

        /// <summary>Number of valid QoS requests sent.</summary>
        internal uint RequestsSent;

        /// <summary>Number of valid, non-duplicate QoS responses received.</summary>
        internal uint ResponsesReceived;

        /// <summary>Average latency (in milliseconds) over all the responses received.</summary>
        /// <remarks>If no responses have been received, will return QosResult.InvalidLatencyValue.</remarks>
        internal uint AverageLatencyMs => (ResponsesReceived > 0) ? (AggregateLatencyMs / ResponsesReceived) : InvalidLatencyValue;

        /// <summary>Percentage of packet loss from 0.0f - 1.0f (0 - 100%).</summary>
        /// <remarks>If no requests have been sent, will return QosResult.InvalidPacketLossValue.</remarks>
        internal float PacketLoss => (RequestsSent == 0 || ResponsesReceived > RequestsSent) ? InvalidPacketLossValue : 1.0f - (ResponsesReceived / (float)RequestsSent);

        /// <summary>Number of requests ignored due to being invalid (wrote fewer than Length bytes).</summary>
        internal uint InvalidRequests;

        /// <summary>Number of responses discarded due to being invalid (too small, too big, etc.).</summary>
        internal uint InvalidResponses;

        /// <summary>Number of duplicate responses that were ignored.</summary>
        internal uint DuplicateResponses;

        /// <summary>Type of FlowControl set on response (if any).</summary>
        internal FcType FcType;

        /// <summary>Units of Flow Control for given FcType (if any).</summary>
        internal byte FcUnits;

        internal uint AggregateLatencyMs; // Sum of all latency measurements for this QoS server (used to compute average latency)

        /// <summary>
        /// Add milliseconds of latency to the overall aggregate for this result.
        /// </summary>
        /// <param name="amountMs">Amount of latency (in milliseconds) to add to the aggregate.</param>
        /// <remarks>
        /// * This is the sum of all latencies for all responses received for this QoS result.
        /// * Used to compute the average latency based on the number of valid responses received.
        /// </remarks>
        internal void AddAggregateLatency(uint amountMs)
        {
            AggregateLatencyMs += amountMs;
        }
    }
}
