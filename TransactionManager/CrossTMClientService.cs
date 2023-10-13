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

            //checks if any transaction manager is down at the moment, if it is we dont propagate
            foreach(int timeSlot in _transactionManager.TmsClients[tmID].Item2)
            {
                if(timeSlot != _transactionManager.TimeSlot)
                {
                    _transactionManager.TmsClients[tmID].Item1.PropagateLeases(request);
                }
            }
        }
    }
}

