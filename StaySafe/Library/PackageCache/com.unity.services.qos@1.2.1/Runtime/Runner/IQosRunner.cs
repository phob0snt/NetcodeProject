using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Qos.Models;

namespace Unity.Services.Qos.Runner
{
    interface IQosRunner
    {
        // Returning a List for simpler sorting (IList doesn't have a Sort method)
        Task<List<Internal.QosResult>> MeasureQosAsync(IList<QosServer> servers);

        // Returning a List for simpler sorting (IList doesn't have a Sort method)
        Task<List<QosAnnotatedResult>> MeasureQosAsync(IList<QosServiceServer> servers);
    }
}
