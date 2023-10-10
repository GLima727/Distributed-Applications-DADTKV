using Grpc.Core;

namespace DADTKV.leaseManager
{
    class PaxosService : PaxosCommunicationService.PaxosCommunicationServiceBase
    {
        private LeaseManager _leaseManager;

        public PaxosService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }
    }
}

