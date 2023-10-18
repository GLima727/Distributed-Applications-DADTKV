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

        public override Task<LeaseSheetResponse> GetLeaseSheet(LeaseSheetRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(GetLeaseSheetImpl(propagateLeasesRequest));
        }

        public LeaseSheetResponse GetLeaseSheetImpl(LeaseSheetRequest request)
        {
            if (_lastLeaseId >= request.Id)
            {
                return new LeaseSheetResponse();
            }

            _lastLeaseId = request.Id;

            DebugClass.Log("Received a lease sheet");
            
            _transactionManager.LeaseSheets = new List<LeaseSheet>();
            int count = 1;

            foreach (Lease lease in request.LeaseSheet.LeaseSheet_)
            {
                foreach (var content in lease.Leases)
                {
                    DebugClass.Log(content);
                }

                LeaseSheet leaseSheet = new LeaseSheet();
                leaseSheet.tmID = lease.TmId;
                leaseSheet.order = count++;

                leaseSheet.leases = new List<string>(lease.Leases);
                _transactionManager.AddLeaseSheet(leaseSheet);
                foreach (string partOfLease in lease.Leases)
                {
                    _transactionManager.RemoveMissingLease(partOfLease);
                    _transactionManager.AddLeaseToList(partOfLease);
                }
            }

            DebugClass.Log("Sent dignal to thread.");
            _transactionManager.Signal.Set();

            return new LeaseSheetResponse();
        }

    }
}

