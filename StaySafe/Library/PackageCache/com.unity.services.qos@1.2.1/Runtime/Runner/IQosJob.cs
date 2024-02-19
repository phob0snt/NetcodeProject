using Unity.Collections;
using Unity.Jobs;

namespace Unity.Services.Qos.Runner
{
    internal interface IQosJob : IJob
    {
        // Added this to the interface, since the extension method in IJobExtensions.JobStruct made it impossible
        // to mock using an interface (it requires T to be a struct as well as an IJob)
        public JobHandle Schedule<T>(JobHandle dependsOn = default) where T : struct, IJob;

        public void Dispose();

        public NativeArray<Unity.Networking.QoS.InternalQosResult> QosResults { get; }
    }
}
