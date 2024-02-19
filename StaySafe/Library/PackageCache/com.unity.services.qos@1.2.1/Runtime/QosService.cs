using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Unity.Services.Qos.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Unity.Services.Qos
{
    /// <summary>
    /// Provides an entry point to the QoS Service, enabling clients to measure the quality of service (QoS) in terms
    /// of latency and packet loss for various regions.
    /// </summary>
    /// <example>
    /// <code lang="cs">
    /// <![CDATA[
    /// using System;
    /// using Unity.Services.Authentication;
    /// using Unity.Services.Core;
    /// using Unity.Services.Qos;
    /// using UnityEngine;
    ///
    /// namespace MyPackage
    /// {
    ///     public class MySample : MonoBehaviour
    ///     {
    ///         public async void RunQos()
    ///         {
    ///             try
    ///             {
    ///                 await UnityServices.InitializeAsync();
    ///                 await AuthenticationService.Instance.SignInAnonymouslyAsync();
    ///                 var serviceName = "multiplay";
    ///                 var qosResults = await QosService.Instance.GetSortedQosResultsAsync(serviceName, null);
    ///             }
    ///             catch (Exception e)
    ///             {
    ///                 Debug.Log(e);
    ///             }
    ///         }
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public static class QosService
    {
        /// <summary>
        /// A static instance of the QoS Service.
        /// </summary>
        public static IQosService Instance { get; internal set; }
    }

    /// <summary>
    /// An interface that allows access to QoS measurements. Your game code should use this interface through
    /// `QosService.Instance`.
    /// </summary>
    public interface IQosService
    {
        /// <summary>
        /// Gets sorted QoS measurements the specified service and regions.
        /// </summary>
        /// <remarks>
        /// `GetSortedQosResultsAsync` doesn't consider the returned regions until applying the services and regions filters.
        ///
        /// If you specify regions, it only includes those regions.
        /// </remarks>
        /// <param name="service">The service to query regions for QoS. `GetSortedQosResultsAsync` only uses measures
        /// regions configured for the specified service.</param>
        /// <param name="regions">The regions to query for QoS. If not null or empty, `GetSortedQosResultsAsync` only uses
        /// regions in the intersection of the specified service and the specified regions for measurements.</param>
        /// <returns>Returns the sorted list of QoS results, ordered from best to worst.</returns>
        Task<IList<IQosResult>> GetSortedQosResultsAsync(string service, IList<string> regions);

        /// <summary>
        /// Gets sorted QoS measurements for Relay service.
        /// </summary>
        /// <remarks>
        /// If you specify regions, it only includes those regions.
        /// </remarks>
        /// <param name="regions">The regions to query for QoS. If not null or empty, `GetSortedRelayQosResultsAsync` only
        /// uses the specified regions for measurements.</param>
        /// <returns>Returns the sorted list of QoS results, ordered from best to worst.</returns>
        Task<IList<IQosAnnotatedResult>> GetSortedRelayQosResultsAsync(IList<string> regions);

        /// <summary>
        /// Gets sorted QoS measurements for Multiplay service.
        /// </summary>
        /// <remarks>
        /// The fleet ID must be a valid Multiplay fleet ID.
        /// </remarks>
        /// <param name="fleet">The fleet to query for QoS. `GetSortedMultiplayQosResultsAsync` only uses QoS servers
        /// in the regions of the fleet for measurements. At least one fleet ID must be passed</param>
        /// <returns>Returns the sorted list of QoS results, ordered from best to worst.</returns>
        Task<IList<IQosAnnotatedResult>> GetSortedMultiplayQosResultsAsync(IList<string> fleet);
    }

    /// <summary>
    /// Represents the results of QoS measurements for a given region.
    /// </summary>
    public interface IQosResult
    {
        /// <summary>
        /// The identifier for the service's region used in this set of QoS measurements.
        /// </summary>
        /// <value>A string containing the region name.
        /// </value>
        public string Region { get; }

        /// <summary>
        /// Average latency of QoS measurements to the region.
        /// </summary>
        /// <remarks>
        /// The latency is determined by measuring the time between sending a packet and receiving the response for that packet,
        /// then taking the average for all responses received. Only packets for which a response was received are
        /// considered in the calculation.
        /// </remarks>
        /// <value>A positive integer, in milliseconds.</value>
        public int AverageLatencyMs { get; }

        /// <summary>
        /// Percentage of packet loss observed in QoS measurements to the region.
        /// </summary>
        /// <remarks>
        /// Packet loss is determined by counting the number of packets for which a response was received from the QoS server,
        /// then taking the percentage based on the total number of packets sent.
        /// </remarks>
        /// <value>A positive flow value. The range is 0.0f - 1.0f (0 - 100%).</value>
        public float PacketLossPercent { get; }
    }

    /// <summary>
    /// Represents the results of QoS measurements for a given region with additional annotations.
    /// </summary>
    public interface IQosAnnotatedResult : IQosResult
    {
        /// <summary>
        /// The results annotations.
        /// </summary>
        /// <value>A dictionary of additional information.
        /// </value>
        public Dictionary<string, List<string>> Annotations { get; }
    }
}
