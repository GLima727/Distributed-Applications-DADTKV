namespace DADTKV.transactionManager
{
    class TMLMService
    {
        private TransactionManager _transactionManager;
        public TMLMService(TransactionManager transactionManager) 
        {
            this._transactionManager = transactionManager;
        }

        public void RequestLeases()
        {
            LeaseRequest requestLease = new LeaseRequest();
            Lease lease = new Lease();
            lease.TmId = _transactionManager.Id;
            lease.Leases.AddRange(_transactionManager.LeasesMissing);
            requestLease.LeaseDetails = lease;

            foreach (var lm in _transactionManager.LmsClients)
            {
                lm.Value.ProcessLeaseRequest(requestLease);
            }
        }
    }
}
