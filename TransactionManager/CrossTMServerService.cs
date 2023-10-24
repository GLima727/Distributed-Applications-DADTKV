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

            //if the request is older than the tm or the tm is the one that created the request dont do anything
            if (request.TimeStamp < _transactionManager.TimeSlot || request.Sender == _transactionManager.Id)
            {
                return new URBroadCastReply();
            }
           else
            {
                DebugClass.Log($"Receiving");

                //se nao tiver recebido deste já
                if (!_transactionManager.AcksReceived.Contains(request.Sender)) {
                    _transactionManager.AcksReceived.Add(request.Sender);

                    if (_transactionManager.AcksReceived.Count < _transactionManager.TmsClients.Count / 2 + 1)
                    {
                        //update values in replica
                        foreach(DADIntCopy message in request.Message)
                        {
                            DebugClass.Log($"está a mudar {message.Value}{message.Key}");

                            _transactionManager.DadInts[message.Key] = message.Value;
                        }
                    }
                    if(_transactionManager.AcksReceived.Count == 1)
                    {
                        DebugClass.Log($"Receiving DAdiNTs");

                        //transmite para os outros apenas uma vez
                        foreach (KeyValuePair<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> tm
                            in _transactionManager.TmsClients)
                        {
                            //dont broadcast the message to the one that created it
                            if (tm.Key == request.Sender)
                            {
                                continue;
                            }
                            URBroadCastRequest urbroadcastRequest = new URBroadCastRequest();

                            urbroadcastRequest.Sender = request.Sender;
                            urbroadcastRequest.Message.AddRange(request.Message);
                            urbroadcastRequest.TimeStamp = _transactionManager.TimeSlot;
                            tm.Value.Item1.URBroadCast(urbroadcastRequest);
                        }
                    }

                }
            }



            URBroadCastReply reply = new URBroadCastReply();
            return reply;
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
                    _transactionManager.RemoveMissingLease(resourceLease);
                    _transactionManager.AddLeaseToAvailableList(resourceLease);
                }

                if (_transactionManager.LeasesMissing.Count == 0)
                {
                    _transactionManager.TransactionManagerSignal.Set();
                }
            }
            else if (request.Id > _lastPropagateId)
            {
                lock (this)
                {
                    _lastPropagateId = request.Id;
                }

                PropagateLeasesRequest progRequest = new PropagateLeasesRequest();
                progRequest.Lease = request.Lease;
                progRequest.Id = request.Id;

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

