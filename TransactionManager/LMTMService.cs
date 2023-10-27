using Grpc.Core;

namespace DADTKV.transactionManager
{
    class LMTMService : LMTMCommunicationService.LMTMCommunicationServiceBase
    {
        private TransactionManager _transactionManager;

        private int _lastLeaseId = -1;

        public LMTMService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<ReceiveLeaseListResponse> ReceiveLeaseList(ReceiveLeaseListRequest request, ServerCallContext context)
        {
            return Task.FromResult(ReceiveLeaseListImpl(request));
        }

        public ReceiveLeaseListResponse ReceiveLeaseListImpl(ReceiveLeaseListRequest request)
        {
            DebugClass.Log("[LM - TM] Received a lease sheet.");
            Monitor.Enter(_transactionManager.LMTMLock);

            lock (this)
            {
                if (_lastLeaseId >= request.RequestId)
                {
                    DebugClass.Log($"[LM - TM] Ignore this lease sheet because last_lease_id={_lastLeaseId} >= lease_id_reveived={request.RequestId}.");
                    Monitor.Exit(_transactionManager.LMTMLock);
                    return new ReceiveLeaseListResponse();
                }

                _transactionManager.NumberLms++;

                if (_transactionManager.NumberLms < _transactionManager.LmsClients.Count / 2)
                {
                    DebugClass.Log($"[LM - TM] Still needs {_transactionManager.LmsClients.Count / 2 - _transactionManager.NumberLms}");
                    Monitor.Exit(_transactionManager.LMTMLock);
                    return new ReceiveLeaseListResponse();
                }

                DebugClass.Log($"[LM - TM] Learnt");
                _lastLeaseId = request.RequestId;
            }


            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();

            DebugClass.Log("[LM - TM] [Send resources]");
            foreach (var resource in _transactionManager.LeasesAvailable)
            {
                foreach (var lease in request.LeaseList.Leases.ToList())
                {
                    if (lease.LeasedResources.Contains((resource)))
                    {
                        DebugClass.Log($"[LM - TM] [Send resources] Needs to send {resource} to {lease.TmId}.");
                        // Theorodical this if is useless
                        if (!leasesToSend.ContainsKey(lease.TmId))
                        {
                            leasesToSend[lease.TmId] = new List<string>();
                        }

                        if (lease.TmId != _transactionManager.Id)
                        {
                            leasesToSend[lease.TmId].Append(resource);
                        }

                        break;
                    }
                }
            }

            DebugClass.Log("[LM - TM] [Send resources] propagate resources.");
            foreach (var val in leasesToSend)
            {
                _transactionManager.PropagateLeaseResource(val.Key, val.Value);
            }

            // Check if i am going to do something
            bool haveTrans = false;
            foreach (var l in request.LeaseList.Leases.ToList())
            {
                if (l.TmId == _transactionManager.Id)
                {
                    haveTrans = true;
                }
            }
            if (!haveTrans)
            {
                Monitor.Exit(_transactionManager.LMTMLock);
                return new ReceiveLeaseListResponse();
            }

            // Run epoch run
            try
            {
                DebugClass.Log("[LM - TM] Set signal.");
                _transactionManager.CurrentRoundPaxos += 1;
                _transactionManager.TransactionEpochList[_transactionManager.CurrentRoundPaxos - 1].EpochSignal.Set();
                _transactionManager.TransactionEpochList[_transactionManager.CurrentRoundPaxos - 1].Run(request.LeaseList.Leases.ToList());
            }
            catch (Exception e)
            {
                DebugClass.Log(e.Message);
            }
            Monitor.Exit(_transactionManager.LMTMLock);
            DebugClass.Log("[LM - TM] Exit.");
            return new ReceiveLeaseListResponse();
        }
    }
}

