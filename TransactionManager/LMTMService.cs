using Grpc.Core;

namespace DADTKV.transactionManager
{
    class LMTMService : LMTMCommunicationService.LMTMCommunicationServiceBase
    {
        private TransactionManager _transactionManager;

        private int _lastLeaseId = 0;

        public LMTMService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<ReceiveLeaseListResponse> ReceiveLeaseList(ReceiveLeaseListRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReceiveLeaseListImpl(request));
        }

        public ReceiveLeaseListResponse ReceiveLeaseListImpl(ReceiveLeaseListRequest request)
        {
            if (_lastLeaseId >= request.RequestId)
            {
                return new ReceiveLeaseListResponse();
            }
            _transactionManager.NumberLms++;
            if (_transactionManager.NumberLms < _transactionManager.LmsClients.Count / 2)
            {
                return new ReceiveLeaseListResponse();
            }

            _lastLeaseId = request.RequestId;
            DebugClass.Log("Received a lease sheet");
            
            _transactionManager.LeaseSheet = request.LeaseList.Leases.ToList();

            DebugClass.Log("Sent signal to thread.");
            _transactionManager.Signal.Set();

            return new ReceiveLeaseListResponse();
        }
    }
}

