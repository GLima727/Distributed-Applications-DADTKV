using Grpc.Core;

namespace DADTKV.leaseManager
{
    class ManagerService : LeaseManagerServie.LeaseManagerServieBase
    {
        private LeaseManager _leaseManager;

        public ManagerService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }

        public override Task<LeaseRequestReply> RequestLease(RequestLeaseRequest request, ServerCallContext context)
        {
            return Task.FromResult(RequestLeaseImpl(request));
        }

        public LeaseRequestReply RequestLeaseImpl(RequestLeaseRequest request)
        {
            return new LeaseRequestReply();
        }
    }
}

