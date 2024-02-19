using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Internal;
using Unity.Services.Core.Telemetry.Internal;
using Unity.Services.Qos.Apis.QosDiscovery;

using Unity.Services.Qos.Http;
using Unity.Services.Qos.Internal;
using Unity.Services.Qos.Runner;
using Unity.Services.Core.Configuration.Internal;

namespace Unity.Services.Qos
{
    class QosPackageInitializer : IInitializablePackage
    {
        const string PackageName = "com.unity.services.qos";
        const string k_CloudEnvironmentKey = "com.unity.services.core.cloud-environment";
        const string k_StagingEnvironment = "staging";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            var package = new QosPackageInitializer();
            package.Register(CoreRegistry.Instance);
        }

        /// <summary>
        /// A helper method to easily register the package with all its dependencies/provisions to a given registry.
        /// </summary>
        /// <param name="registry">
        /// The registry to register your package to.
        /// </param>
        internal void Register(CoreRegistry registry)
        {
            // Pass an instance of this class to Core
            registry.RegisterPackage(this)
                .DependsOn<IAccessToken>()
                .DependsOn<IMetricsFactory>()
                .DependsOn<IProjectConfiguration>()
                .ProvidesComponent<IQosResults>();
        }

        public Task Initialize(CoreRegistry registry)
        {
            var projectConfiguration = registry.GetServiceComponent<IProjectConfiguration>();
            var httpClient = new HttpClient();

            var accessTokenQosDiscovery = registry.GetServiceComponent<IAccessToken>();
            var metricsFactory = registry.GetServiceComponent<IMetricsFactory>();
            var metrics = metricsFactory.Create(PackageName);

            // Set up internal QoS Discovery API client & config
            QosDiscoveryService.Instance = new InternalQosDiscoveryService(GetHost(projectConfiguration), httpClient, accessTokenQosDiscovery);

            // Set up public QoS interface
            var wrappedQosService = new WrappedQosService(QosDiscoveryService.Instance.QosDiscoveryApi,
                new BaselibQosRunner(), accessTokenQosDiscovery, metrics);
            QosService.Instance = wrappedQosService;
            registry.RegisterServiceComponent<IQosResults>(new QosResults(wrappedQosService));

            return Task.CompletedTask;
        }

        string GetHost(IProjectConfiguration projectConfiguration)
        {
            var cloudEnvironment = projectConfiguration?.GetString(k_CloudEnvironmentKey);

            switch (cloudEnvironment)
            {
                case k_StagingEnvironment:
                    return "https://qos-discovery-stg.services.api.unity.com";
                default:
                    return "https://qos-discovery.services.api.unity.com";
            }
        }
    }

    /// <summary>
    /// InternalQosDiscoveryService
    /// </summary>
    class InternalQosDiscoveryService : IQosDiscoveryService
    {
        const int RequestTimeout = 10;
        const int NumRetries = 4;

        /// <summary>
        /// Constructor for InternalQosDiscoveryService
        /// </summary>
        /// <param name="httpClient">The HttpClient for InternalQosDiscoveryService.</param>
        /// <param name="accessToken">The Authentication token for the service.</param>
        internal InternalQosDiscoveryService(string host, HttpClient httpClient, IAccessToken accessToken = null)
        {
            Configuration = new Configuration(host, RequestTimeout, NumRetries, null);

            QosDiscoveryApi = new QosDiscoveryApiClient(httpClient, accessToken, Configuration);
        }

        public IQosDiscoveryApiClient QosDiscoveryApi { get; set; }

        /// <summary> Configuration properties for the service.</summary>
        public Configuration Configuration { get; set; }
    }
}
