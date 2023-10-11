using Grpc.Core;

namespace DADTKV.transactionManager
{
    class CrossTMServerService : CrossServerTransactionManagerService.CrossServerTransactionManagerServiceBase
    {
        private TransactionManager _transactionManager;

        public CrossTMServerService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<PropagateLeasesReply> PropagateLeases(PropagateLeasesRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(PropagateLeasesImpl(propagateLeasesRequest));
        }

        public PropagateLeasesReply PropagateLeasesImpl(PropagateLeasesRequest request)
        {
            foreach (string lease in request.Leases)
            {
                _transactionManager.LeasesMissing.Remove(lease);
            }
            if (_transactionManager.LeasesMissing.Count == 0)
            {
                _transactionManager.TransactionManagerSignals[_transactionManager.Id].Set();
            }


            PropagateLeasesReply reply = new PropagateLeasesReply();
            return reply;
        }

    }
}

