using Grpc.Core;

namespace DADTKV.transactionManager
{
    class CrossTMServerService : CrossServerTransactionManagerService.CrossServerTransactionManagerServiceBase
    {
        private TransactionManager _transactionManager;
        private int _lastPropagateId = 0;


        public CrossTMServerService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<PropagateLeasesReply> PropagateLeases(PropagateLeasesRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(PropagateLeasesImpl(propagateLeasesRequest));
        }

        public PropagateLeasesReply PropagateLeasesImpl(PropagateLeasesRequest request)
        {

            DebugClass.Log("Received a Lease from progate.");
            PropagateLeasesReply reply = new PropagateLeasesReply();
            foreach (var tm in _transactionManager.TmsClients)
            {
                if (tm.Key == request.SenderId && tm.Value.Item2.Contains(_transactionManager.NRound))
                {
                    return reply;
                }
            }

            DebugClass.Log("And i dont ignore");

            if (request.Lease.TmId == _transactionManager.Id)
            {
                DebugClass.Log("---- The Lease is for me :).");
                foreach (string resourceLease in request.Lease.LeasedResources)
                {
                    _transactionManager.RemoveMissingLease(resourceLease);
                    _transactionManager.AddLeaseToAvailableList(resourceLease);
                }

                if (_transactionManager.LeasesMissing.Count == 0)
                {
                    DebugClass.Log("---- We have all lets gooooo.");
                    _transactionManager.TransactionManagerSignal.Set();
                }
            }
            else if (request.Id > _lastPropagateId)
            {
                DebugClass.Log("---- The Lease is not for me :_.");
                lock (this)
                {
                    _lastPropagateId = request.Id;
                }

                PropagateLeasesRequest progRequest = new PropagateLeasesRequest();
                progRequest.Lease = request.Lease;
                progRequest.Id = request.Id;
                progRequest.SenderId = _transactionManager.Id;

                // checks if any transaction manager can respond to it in this timeslot
                lock (_transactionManager)
                {
                    foreach (var tm in _transactionManager.TmsClients)
                    {
                        if (!tm.Value.Item2.Contains(_transactionManager.TimeSlot))
                        {
                            //if you dont suspect the tm at this timeslot you can ask for the leases
                            tm.Value.Item1.PropagateLeases(progRequest);
                        }
                    }
                }
            }

            return reply;
        }

    }
}

