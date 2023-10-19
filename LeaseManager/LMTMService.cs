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

        public override Task<ReceiveLeaseResponse> ReceiveLease(ReceiveLeaseRequest leaseRequest, ServerCallContext context)
        {
            return Task.FromResult(ProcLeaseReqImpl(leaseRequest));
        }

        public ReceiveLeaseResponse ProcLeaseReqImpl(ReceiveLeaseRequest leaseRequest)
        {
            DebugClass.Log("Received a Lease request.");
            _leaseManager.AddLeaseToBuffer(leaseRequest.Lease);
            return new ReceiveLeaseResponse();
        }

    }
}

