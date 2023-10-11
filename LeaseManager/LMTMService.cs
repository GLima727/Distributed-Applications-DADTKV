using Grpc.Core;

namespace DADTKV.leaseManager
{
    class LMTMService : LMTMCommunicationService.LMTMCommunicationServiceBase
    {
        private LeaseManager _leaseManager;

        public LMTMService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }

        public override Task<LeaseResponse> ProcessLeaseRequest(LeaseRequest leaseRequest, ServerCallContext context)
        {
            return Task.FromResult(ProcLeaseReqImpl(leaseRequest));
        }

        public LeaseResponse ProcLeaseReqImpl(LeaseRequest leaseRequest)
        {
            DebugClass.Log("Received a Lease request.");
            _leaseManager.AddLeaseToBuffer(leaseRequest.LeaseDetails);
            return new LeaseResponse();
        }

    }
}

