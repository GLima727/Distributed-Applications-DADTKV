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
            if (_lastLeaseId >= request.RequestId) return new ReceiveLeaseListResponse();

            _transactionManager.NumberLms++;

            if (_transactionManager.NumberLms < _transactionManager.LmsClients.Count / 2) return new ReceiveLeaseListResponse();

            _lastLeaseId = request.RequestId;
            DebugClass.Log("Received a lease sheet");

            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();

            DebugClass.Log("Check if it does to send resources from the least round.");
            foreach (var resource in _transactionManager.LeasesAvailable)
            {
                foreach (var lease in request.LeaseList.Leases.ToList())
                {
                    if (lease.LeasedResources.Contains((resource)))
                    {
                        DebugClass.Log($"Needs to send {resource} to {lease.TmId}.");
                        // Theorodical this if is useless
                        if (lease.TmId != _transactionManager.Id)
                        {
                            leasesToSend[lease.TmId].Append(resource);
                        }

                        break;
                    }
                }
            }

            DebugClass.Log("Send resources.");
            foreach (var val in leasesToSend)
            {
                _transactionManager.PropagateLeaseResource(val.Key, val.Value);
            }

            _transactionManager.LeaseSheet = request.LeaseList.Leases.ToList();

            DebugClass.Log("Sent signal to thread.");
            _transactionManager.TransactionQueue.Dequeue().Set();
            return new ReceiveLeaseListResponse();
        }
    }
}

