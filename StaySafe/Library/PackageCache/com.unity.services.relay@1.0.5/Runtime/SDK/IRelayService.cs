using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;

namespace Unity.Services.Relay
{
    /// <summary>
    /// The Relay Allocations Service enables clients to connect to relay servers. Once connected, they are able to communicate with each other, via the relay servers, using the bespoke relay binary protocol.
    /// </summary>
    public interface IRelayService
    {
        /// <summary>
        /// Creates an allocation on an available relay server.
        /// If region is provided, the allocation service will look for a relay server in that region, otherwise, it will use the default region.
        /// The allocations service will determine the best available relay server, based upon available capacity and server health for the resolved region.
        /// In the event that a relay server could not be found during the lifetime of this request, the caller should retry.
        /// This endpoint returns a unique ID for the allocation (which can be used to generate a join code), details of the relay server which was chosen for the allocation, encrypted connection data for the caller and a key required for building the BIND message HMAC.
        /// </summary>
        /// <remarks>
        /// The player who creates this allocation, acts as a host. Once the allocation has been created, the host calls GetJoinCode to get a join code which they can then share with other players out of band.
        /// </remarks>
        /// <param name="maxConnections">Indicates the maximum number of connections that the client will allow to communicate with them. It will also be used in order to find a relay with sufficient capacity.</param>
        /// <param name="region">Indicates the preferred region to create the allocation.</param>
        /// <returns>An allocation (which can be used to generate a join code), details of the relay server which was chosen for the allocation, encrypted connection data for the caller and a key required for building the BIND message HMAC.</returns>
        Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null);

        /// <summary>
        /// Returns a "join code" for the given allocation ID.
        /// A "join code" can be thought of as a short-hand lookup for a clients connection data. The connection data will be retrieved from the clients allocation via a mapping between the allocation ID and the join code.
        /// Join codes are intended to be used for players to coordinate themselves into a logical room or session. The player who created the initial allocation, acts as a host. The host calls this method to get a join code which they can then share with other players out of band.
        /// The join code can then be used by other players by having them call JoinAllocation in order to connect to a relay server with which they can communicate with the host.
        /// The host is not required to make a BIND call to the relay server before generating a join code to share.
        /// </summary>
        /// <param name="allocationId">The allocation ID of the previously created allocation.</param>
        /// <returns>A "join code" to be shared with other players so they can connect to the relay and communicate with the host.</returns>
        Task<string> GetJoinCodeAsync(Guid allocationId);

        /// <summary>
        /// Allows players to join a relay server using a join code.
        /// A join code is a shorthand code used to lookup the connection data of the player who created it, referred to as the "host". By performing this lookup, the IP and port of the relay the original player connected to can be determined.
        /// The player calling can then be allocated to the same relay.
        /// This endpoint returns the details of this allocation and also the connection details of the host player. This can then be used to establish communication with the host.
        /// </summary>
        /// <param name="joinCode">The "join code" the host player shared with the other players.</param>
        /// <returns>An allocation details of the relay server which was chosen for the allocation, encrypted connection data for the caller and a key required for building the BIND message HMAC.</returns>
        Task<JoinAllocation> JoinAllocationAsync(string joinCode);

        /// <summary>
        /// Lists all available regions with relay servers. This list is more stable than actual relay server availability, that is, it is not guarantied that there will be a relay server ready to handle allocations in every region listed.
        /// </summary>
        /// <returns>A list of regions where relay servers might be available.</returns>
        Task<List<Region>> ListRegionsAsync();
    }

    /// <summary>
    /// This interface is marked for deprecation and may be removed in future versions. Please use IRelayService instead.
    /// The Relay Allocations Service enables clients to connect to relay servers. Once connected, they are able to communicate with each other, via the relay servers, using the bespoke relay binary protocol.
    /// </summary>
    //[Obsolete("This interface is marked for deprecation and may be removed in future versions. Please use IRelayService instead.")]
    public interface IRelayServiceSDK : IRelayService
    {

    }
}