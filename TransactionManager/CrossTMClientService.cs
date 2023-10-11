using Grpc.Core;

namespace DADTKV.transactionManager
{
    class CrossTMClientService
    {
        private TransactionManager _transactionManager;

        public CrossTMClientService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public void PropagateLease(string tmID, List<string> leases)
        {
            PropagateLeasesRequest request = new PropagateLeasesRequest();
            request.Leases.AddRange(leases);

            _transactionManager.TmsClients[tmID].PropagateLeases(request);
        }
    }
}

