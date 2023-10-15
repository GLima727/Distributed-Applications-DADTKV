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

            //checks if any transaction manager can respond to it in this timeslot

            if( !_transactionManager.TmsClients[tmID].Item2.Contains( _transactionManager.TimeSlot))
            {
                //if you dont suspect the tm at this timeslot you can ask for the leases
                _transactionManager.TmsClients[tmID].Item1.PropagateLeases(request);

            }
     
            }
        }
    }
}

