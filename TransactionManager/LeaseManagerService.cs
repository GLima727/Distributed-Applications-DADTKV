using Grpc.Core;

namespace DADTKV.transactionManager
{
    class LeaseManagerServicings : LeaseManagerServicing.LeaseManagerServicingBase
    {
        private TransactionManager _transactionManager;

        public LeaseManagerServicings(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<LeaseSheetReply> LeaseSheet(LeaseSheetMessage propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(LeaseSheetImpl(propagateLeasesRequest));
        }

        public LeaseSheetReply LeaseSheetImpl(LeaseSheetMessage propagateLeasesRequest)
        {



            return new LeaseSheetReply();
        }

    }
}

