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
            DebugClass.Log("Received a lease sheet");
            _transactionManager.LeaseSheets = new List<LeaseSheet>();
            int count = 0;
            DebugClass.Log(request.LeaseSheet.LeaseSheet_.Count().ToString());
            /*
            foreach (Lease lease in request.LeaseSheet)
            {
                DebugClass.Log(lease.Leases.Count().ToString());
                foreach (var content in lease.Leases) {
                    DebugClass.Log(content);
                }
                LeaseSheet leaseSheet = new LeaseSheet();
                leaseSheet.tmID = lease.TmId;
                leaseSheet.order = count++;

                leaseSheet.leases = new List<string>(lease.Leases);
                _transactionManager.LeaseSheets.Add(leaseSheet);
            }
            _transactionManager.Signal.Set();
            */

            return new LeaseSheetResponse();
        }

    }
}

