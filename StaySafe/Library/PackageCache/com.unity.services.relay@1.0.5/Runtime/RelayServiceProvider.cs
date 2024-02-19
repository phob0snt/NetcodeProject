using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Qos.Internal;
using Unity.Services.Relay.Apis.RelayAllocations;
using Unity.Services.Relay.Http;
using UnityEngine;

namespace Unity.Services.Relay
{
    internal class RelayServiceProvider : IInitializablePackage
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            // Pass an instance of this class to Core
            var generatedPackageRegistry =
                CoreRegistry.Instance.RegisterPackage(new RelayServiceProvider());
            // And specify what components it requires, or provides.
            generatedPackageRegistry.DependsOn<IAccessToken>();
            generatedPackageRegistry.DependsOn<IProjectConfiguration>();
            generatedPackageRegistry.DependsOn<IQosResults>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            var httpClient = new HttpClient();

            var accessToken = registry.GetServiceComponent<IAccessToken>();
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();
            var qosResults = registry.GetServiceComponent<IQosResults>();

            if (accessToken != null)
            {
                RelayServiceSdk.Instance = new InternalRelayService(httpClient, projectConfiguration, accessToken, qosResults);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// InternalRelayService
    /// </summary>
    internal class InternalRelayService : IRelayServiceSdk
    {
        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";
        const string k_StagingEnvironment = "staging";

        /// <summary> Instance of IRelayAllocationsApiClient interface</summary>
        public IRelayAllocationsApiClient AllocationsApi { get; set; }

        /// <summary> Configuration properties for the service.</summary>
        public Configuration Configuration { get; set; }
        public IAccessToken AccessToken { get; set; }

        public IQosResults QosResults { get; set; }

        /// <summary>
        /// Constructor for InternalRelayService
        /// </summary>
        /// <param name="httpClient">The HttpClient for InternalRelayService.</param>
        /// <param name="accessToken">The Authentication token for the service.</param>
        /// <param name="qosResults">The component used to measure QoS.</param>
        public InternalRelayService(HttpClient httpClient, IProjectConfiguration projectConfiguration = null, IAccessToken accessToken = null, IQosResults qosResults = null)
        {
            AllocationsApi = new RelayAllocationsApiClient(httpClient, accessToken);
            Configuration = new Configuration(GetHost(projectConfiguration), 10, 4, null);
            AccessToken = accessToken;
            QosResults = qosResults;
        }

        string GetHost(IProjectConfiguration projectConfiguration)
        {
            var cloudEnvironment = projectConfiguration?.GetString(k_CloudEnvironmentKey);

            switch (cloudEnvironment)
            {
                case k_StagingEnvironment:
                    return "https://relay-allocations-stg.services.api.unity.com";
                default:
                    return "https://relay-allocations.services.api.unity.com";
            }
        }
    }
}
