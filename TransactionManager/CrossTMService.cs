using Grpc.Core;

namespace DADTKV.transactionManager
{
    class CrossTMService : CrossServerTransactionManagerService.CrossServerTransactionManagerServiceBase
    {
        private TransactionManager _transactionManager;

        public CrossTMService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<PropagateLeasesReply> PropagateLeases(PropagateLeasesRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(PropagateLeasesImpl(propagateLeasesRequest));
        }

        public PropagateLeasesReply PropagateLeasesImpl(PropagateLeasesRequest propagateLeasesRequest)
        {
            return new PropagateLeasesReply();
        }

    }
}

