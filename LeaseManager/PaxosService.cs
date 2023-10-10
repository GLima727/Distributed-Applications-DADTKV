using Grpc.Core;

namespace DADTKV.leaseManager
{
    class PaxosService : PaxosCommunicaitonService.PaxosCommunicaitonServiceBase
    {
        private LeaseManager _leaseManager;

        public PaxosService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }
    }
}

