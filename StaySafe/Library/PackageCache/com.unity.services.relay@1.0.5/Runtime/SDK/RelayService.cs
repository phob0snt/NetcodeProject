using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.Relay.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Unity.Services.Relay
{
    /// <summary>
    /// The entry class of the Relay Allocations Service enables clients to connect to relay servers. Once connected, they are able to communicate with each other, via the relay servers, using the bespoke relay binary protocol.
    /// </summary>
    public static class RelayService
    {
        private static IRelayService service;
        
        /// <summary>
        /// A static instance of the Relay Allocation Client.
        /// </summary>
        public static IRelayService Instance
        {
            get
            {
                if (service != null)
                {
                    return service;
                }

                var serviceSdk = RelayServiceSdk.Instance;
                if (serviceSdk == null) 
                {
                    throw new InvalidOperationException("Attempting to call Relay Services requires initializing Core Registry. Call 'UnityServices.InitializeAsync' first!");
                }

                service = new WrappedRelayService(serviceSdk);
                return service;
            }
        }
    }

    /// <summary>
    /// "Relay class is marked for deprecation. Please use RelayService class instead."
    /// </summary>
    //[Obsolete("Relay class is marked for deprecation. Please use RelayService class instead.")]
    public static class Relay
    {
        /// <summary>
        /// Relay class is marked for deprecation. Please use RelayService.Instance instead.
        /// </summary>
        //[Obsolete("Relay class is marked for deprecation. Please use RelayService.Instance instead.")]
        public static IRelayServiceSDK Instance { get { return (IRelayServiceSDK) RelayService.Instance; } }
    }
}
