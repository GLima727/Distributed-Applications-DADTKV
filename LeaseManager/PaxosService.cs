using Grpc.Core;

namespace DADTKV.leaseManager
{
    class PaxosService : PaxosServerService.PaxosServerServiceBase
    {
        private LeaseManager _leaseManager;

        public PaxosService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }

        public override Task<AcceptResponse> Accept(AcceptRequest acceptRequest, ServerCallContext context)
        {
            return Task.FromResult(AcceptImpl(acceptRequest));
        }

        public AcceptResponse AcceptImpl(AcceptRequest accept)
        {
            return new AcceptResponse();
        }

        public override Task<PrepareResponse> Prepare(PrepareRequest prepareRequest, ServerCallContext context)
        {
            return Task.FromResult(PrepareImpl(prepareRequest));
        }

        public PrepareResponse PrepareImpl(PrepareRequest prepareRequest)
        {
            return new PrepareResponse();
        }
    }
}

