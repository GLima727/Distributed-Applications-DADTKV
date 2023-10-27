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
        public override Task<URBroadCastReply> URBroadCast(URBroadCastRequest urbRequest, ServerCallContext context)
        {
            return Task.FromResult(URBroadCastImpl(urbRequest));
        }

        public URBroadCastReply URBroadCastImpl(URBroadCastRequest request)
        {
            DebugClass.Log($"[URBroadCast Server] RECEIVING URBS FROM {request.Sender} ");

            //if the request is older than the tm or the tm is the one that created the request dont do anything
            if (request.Sender == _transactionManager.Id)
            {
                DebugClass.Log($"[URBroadCast Server] RECEIVING URBS but the sender is the same as me {request.Sender}");
                return new URBroadCastReply();
            }
            else
            {

                if (!_transactionManager.AcksReceived.ContainsKey(request.Sender))
                {
                    _transactionManager.AcksReceived[request.Sender] = 0;
                }


                _transactionManager.AcksReceived[request.Sender] += 1;

                DebugClass.Log($"[URBroadCast Server] COUNT? {_transactionManager.AcksReceived[request.Sender]}");

                if (_transactionManager.AcksReceived[request.Sender] == 1)
                {
                    DebugClass.Log($"[URBroadCast Server]FIRST TIME RECEIVING MEMORY OF {request.Sender}");

                    //transmite para os outros apenas uma vez
                    foreach (KeyValuePair<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> tm
                        in _transactionManager.TmsClients)
                    {

                        DebugClass.Log($"[URBroadCast Server] IM GOING TO SEND TO {tm.Key}");

                        //dont broadcast the message to the one that created it
                        if (tm.Key == request.Sender || tm.Key == _transactionManager.Id)
                        {
                            DebugClass.Log($"[URBroadCast Server] IM NOT GOING TO SEND TO {tm.Key}");

                            continue;
                        }


                        URBroadCastRequest urbroadcastRequest = new URBroadCastRequest();

                        urbroadcastRequest.Sender = request.Sender;
                        urbroadcastRequest.Message.AddRange(request.Message);
                        urbroadcastRequest.TimeStamp = _transactionManager.CurrentRound;
                        tm.Value.Item1.URBroadCast(urbroadcastRequest);
                    }
                }
                if (_transactionManager.AcksReceived[request.Sender] >= _transactionManager.TmsClients.Count / 2)
                {
                    DebugClass.Log($"[URBroadCast Server]RECEIVED FROM MAJORITY MEMORY OF {request.Sender}");

                    //update values in replica
                    foreach (DADInt message in request.Message)
                    {
                        DebugClass.Log($"[URBroadCast Server] change memory to {message.Value} {message.Key} from {request.Sender}");

                        _transactionManager.DadInts[message.Key] = message.Value;
                    }
                    //reinicia e já pode receber desse criador
                }

                if (_transactionManager.AcksReceived[request.Sender] == _transactionManager.TmsClients.Count - 1)
                {
                    _transactionManager.AcksReceived[request.Sender] = 0;
                }
            }

            return new URBroadCastReply();
        }

        public override Task<PropagateLeasesReply> PropagateLeases(PropagateLeasesRequest propagateLeasesRequest, ServerCallContext context)
        {
            return Task.FromResult(PropagateLeasesImpl(propagateLeasesRequest));
        }

        public PropagateLeasesReply PropagateLeasesImpl(PropagateLeasesRequest request)
        {
            DebugClass.Log($"[Propagate Lease] received a lease.");
            DebugClass.Log($"[Propagate Lease] have lock.");
            PropagateLeasesReply reply = new PropagateLeasesReply();

            int current_round = _transactionManager.CurrentRound;

            foreach (var tm in _transactionManager.TmsClients)
            {
                if (tm.Key == request.SenderId && tm.Value.Item2.Contains(current_round))
                {
                    return reply;
                }
            }

            DebugClass.Log($"[Propagate Lease] I dont ignore.");
            if (request.Lease.TmId == _transactionManager.Id)
            {
                try
                {
                    DebugClass.Log("[Propagate Lease] The Lease is for me :).");
                    Monitor.Enter(_transactionManager.TMLock);
                    foreach (string resourceLease in request.Lease.LeasedResources)
                    {
                        _transactionManager.LeasesAvailable.Add(resourceLease);
                    }
                    Monitor.Exit(_transactionManager.TMLock);

                    DebugClass.Log($"[Propagate Lease] Notify currentTrans we received a lease.");
                    _transactionManager.CurrentTrans.SignalLTM.Set();
                }
                catch (Exception e)
                {
                    DebugClass.Log(e.Message);
                }
            }
            else if (request.Id > _lastPropagateId)
            {
                DebugClass.Log($"[Propagate Lease] The Lease is not for me :_.");
                _lastPropagateId = request.Id;

                PropagateLeasesRequest progRequest = new PropagateLeasesRequest();
                progRequest.Lease = request.Lease;
                progRequest.Id = request.Id;
                progRequest.SenderId = _transactionManager.Id;

                DebugClass.Log($"Enter lock _transactionManager");
                // propagate the leases 
                foreach (var tm in _transactionManager.TmsClients)
                {
                    if (!tm.Value.Item2.Contains(current_round))
                    {
                        tm.Value.Item1.PropagateLeases(progRequest);
                    }
                }
            }

            DebugClass.Log("[Propagate Lease] Exit.");
            return reply;
        }

    }
}

