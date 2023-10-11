using Grpc.Core;

namespace DADTKV.transactionManager
{
    class LMTMService : LMTMCommunicationService.LMTMCommunicationServiceBase
    { 
        private TransactionManager _transactionManager;

        public LMTMService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<LeaseSheetResponse> GetLeaseSheet(LeaseSheetRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(GetLeaseSheetImpl(propagateLeasesRequest));
        }

        public LeaseSheetResponse GetLeaseSheetImpl(LeaseSheetRequest request)
        {
            _transactionManager.LeaseSheets = new List<LeaseSheet>();
            int count = 0;
            foreach (Lease lease in request.LeaseSheet)
            {
                LeaseSheet leaseSheet = new LeaseSheet();
                leaseSheet.tmID = lease.TmId;
                leaseSheet.order = count++;

                leaseSheet.leases = new List<string>(lease.Leases);
                _transactionManager.LeaseSheets.Add(leaseSheet);
            }
            _transactionManager.Signal.Set();

            return new LeaseSheetResponse();
        }

    }
}

