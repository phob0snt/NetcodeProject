using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Qos.Internal;

namespace Unity.Services.Qos
{
    class QosResults : IQosResults
    {
        WrappedQosService _qosService;

        internal QosResults(WrappedQosService qosService)
        {
            _qosService = qosService;
        }

        public Task<IList<Internal.QosResult>> GetSortedQosResultsAsync(string service, IList<string> regions)
        {
            return _qosService.GetSortedInternalQosResultsAsync(service, regions);
        }
    }
}
