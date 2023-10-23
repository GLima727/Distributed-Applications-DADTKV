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
            if (request.Lease.TmId == _transactionManager.Id)
            {
                foreach (string resourceLease in request.Lease.LeasedResources)
                {
                    _transactionManager.RemoveLeaseToList(resourceLease);
                    _transactionManager.AddLeaseToList(resourceLease);
                }

                if (_transactionManager.LeasesMissing.Count == 0)
                {
                    // implement queue
                    _transactionManager.TransactionManagerSignal.Set();
                }
            }
            else if (request.Id > _lastPropagateId)
            {
                lock (this)
                {
                    _lastPropagateId++;
                }

                PropagateLeasesRequest progRequest = new PropagateLeasesRequest();
                progRequest.Lease = request.Lease;
                progRequest.Id = request.Id++;

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


            PropagateLeasesReply reply = new PropagateLeasesReply();
            return reply;
        }

    }
}

