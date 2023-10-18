namespace DADTKV.transactionManager
{
    class TMLMService
    {
        private TransactionManager _transactionManager;
        public TMLMService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public static async Task<LeaseResponse> RequestLs(
        LMTMCommunicationService.LMTMCommunicationServiceClient client,
        LeaseRequest request)
        {
            return await client.ProcessLeaseRequestAsync(request);
        }

        public void RequestLeases()
        {
            LeaseRequest requestLease = new LeaseRequest();
            Lease lease = new Lease();
            lease.TmId = _transactionManager.Id;
            lease.Leases.AddRange(_transactionManager.LeasesMissing);
            requestLease.LeaseDetails = lease;

            var tasks = new List<Task<LeaseResponse>>();

            foreach (var val in _transactionManager.LmsClients)
            {
                tasks.Add(RequestLs(val.Value, requestLease));
            }

            Task.WhenAll(tasks);
        }
    }
}
