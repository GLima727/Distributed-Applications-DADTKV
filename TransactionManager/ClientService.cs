using Grpc.Core;

namespace DADTKV.transactionManager
{
    class TransactionInfo
    {
        List<string> missingLeases = new List<string>();
        public List<string> MissingLeases
        {
            get
            {
                lock (this)
                {
                    return missingLeases;
                }
            }
            set
            {
                lock (this)
                {
                    missingLeases = value;
                }
            }
        }

        ManualResetEventSlim signalLSheet = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLSheet
        {
            get
            {
                lock (this)
                {
                    return signalLSheet;
                }
            }
            set
            {
                lock (this)
                {
                    signalLSheet = value;
                }
            }
        }

        ManualResetEventSlim signalLTM = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLTM
        {
            get
            {
                lock (this)
                {
                    return signalLTM;
                }
            }
            set
            {
                lock (this)
                {
                    signalLTM = value;
                }
            }
        }
    }

    class ClientService : ClientServerService.ClientServerServiceBase
    {
        private TransactionManager _transactionManager;

        private int _transId = 0;
        private object _transIdock = new object();

        public int TransactionID
        {
            get { lock (_transIdock) { return _transId; } }
            set { lock (_transIdock) { _transId = value; } }
        }


        public ClientService(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public override Task<ClientTransactionReply> SubmitTransaction(ClientTransactionRequest clientTransactionRequest, ServerCallContext context)
        {
            return Task.FromResult(SubmitTransactionImpl(clientTransactionRequest));
        }

        public bool TmIsDown()
        {
            return _transactionManager.RoundsDowns.Contains(_transactionManager.TimeSlot);
        }

        public ClientTransactionReply SubmitTransactionImpl(ClientTransactionRequest request)
        {
            DebugClass.Log("[SubmitTransactionImpl] Received transaction.");
            int currentTransId = _transId;
            _transId++;

            foreach (KeyValuePair<string, int> dad in _transactionManager.DadInts)
            {
                DebugClass.Log2($"[SubmitTransactionImpl] [DadInts present in memory] <{dad.Key},{dad.Value}>");
            }

            var info = new TransactionInfo();
            _transactionManager.TransactionQueueInfo.Enqueue(info);
            DebugClass.Log($"[SubmitTransactionImpl] {_transactionManager.TransactionQueueInfo.Count}.");

            ClientTransactionReply reply = new ClientTransactionReply();
            if (TmIsDown())
            {
                DebugClass.Log("[SubmitTransactionImpl] This TM is down.");
                return reply;
            }

            DebugClass.Log("[SubmitTransactionImpl] [Read transactions]");
            foreach (string readOp in request.ReadOperations)
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] Needs to access {readOp}.");
                if (!_transactionManager.ContainsLease(readOp))
                {
                    DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] doesn't have {readOp}.");
                    info.MissingLeases.Add(readOp);
                }
            }

            DebugClass.Log("[SubmitTransactionImpl] [Write transactions]");
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] Needs to access {dadInt.Key}.");
                if (!_transactionManager.ContainsLease(dadInt.Key))
                {
                    info.MissingLeases.Add(dadInt.Key);
                    DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] doesn't have {dadInt.Key}.");
                }
            }


            DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] missing {info.MissingLeases.Count} leases");
            if (info.MissingLeases.Count != 0)
            {
                RequestLeases(info.MissingLeases, currentTransId);

                _transactionManager.NumberLms = 0;
                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Sent lease requests.");
                // Wait to receive lease sheet
                info.SignalLSheet.Wait();
                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Received lease sheet.");
                info.SignalLSheet.Reset();

                List<Lease> leaseSheet = _transactionManager.LeaseSheet;
                // send lms for the lease sheet but check if its down

                int lease_index = 0;

                // For each Lease i received
                foreach (Lease lease in leaseSheet)
                {
                    // Check if this lease is for this tmId
                    if (lease.TmId == _transactionManager.Id && lease.TransactionId == currentTransId)
                    {
                        // if is the first dont look back
                        if (lease_index == 0 && _transactionManager.NRound == 1)
                        {
                            _transactionManager.LeasesAvailable = info.MissingLeases;
                            DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] I was the first to receive this lease.");
                        }
                        else
                        {
                            // check if someone have the leases we need
                            var missingLeases = lookBackLeases(lease, lease_index, leaseSheet);
                            var leases_to_add = info.MissingLeases.Except(missingLeases).ToList();
                            info.MissingLeases = missingLeases;

                            foreach (var l in leases_to_add)
                            {
                                if (!_transactionManager.LeasesAvailable.Contains(l))
                                    _transactionManager.LeasesAvailable.Add(l);
                            }

                            foreach (var l in info.MissingLeases)
                            {
                                DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] missing {l}");
                            }

                            if (info.MissingLeases.Count != 0)
                            {
                                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] we need to wait to get leases from others tms");
                                // Wait for others tm to give leases
                                info.SignalLTM.Wait();
                                info.SignalLTM.Reset();
                            }
                        }

                        reply = executeOperations(request);

                        // Send Leases to anyone who needs it
                        DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases]");
                        Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index, leaseSheet);
                        foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                        {
                            DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases] sending {leases.Key}");

                            //im checking the suspicion list inside this
                            //here you ask for leases but dont send the request if you suspect the one you are asking
                            _transactionManager.PropagateLeaseResource(leases.Key, leases.Value);

                            // remove A from ("A","B") and so on
                            foreach (string resource in leases.Value)
                            {
                                _transactionManager.RemoveLeaseFromAvailableList(resource);
                            }
                        }

                        // we don't need to see more leases
                        break;
                    }
                    lease_index++;
                }
            }
            else
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] perform transaction");
                reply = executeOperations(request);
            }

            DebugClass.Log($"[SubmitTransactionImpl] pass to the next in queue");
            if (_transactionManager.TransactionQueueInfo.Count > 0)
            {
                _transactionManager.TransactionQueueInfo.Dequeue().SignalLSheet.Set();
            }

            return reply;
        }

        public Dictionary<string, List<string>> lookAheadLeases(Lease lease, int lease_index, List<Lease> leaseSheet)
        {
            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();
            foreach (string resource in lease.LeasedResources)
            {
                for (int i = lease_index + 1; i < leaseSheet.Count; i++)
                {
                    if (leaseSheet[i].LeasedResources.Contains(resource))
                    {
                        if (leaseSheet[i].TmId != _transactionManager.Id)
                        {
                            // atencao verificar se ele nao esta a criar ids repetidos no dicionario
                            if (!leasesToSend.ContainsKey(leaseSheet[i].TmId))
                            {
                                leasesToSend[leaseSheet[i].TmId] = new List<string>();
                            }
                            leasesToSend[leaseSheet[i].TmId].Add(resource);
                        }
                        break;
                    }
                }

            }

            return leasesToSend;
        }

        public List<string> lookBackLeases(Lease lease, int lease_index, List<Lease> leaseSheet)
        {
            List<string> missingLeases = new List<string>();

            // for each resource we want
            // A B
            DebugClass.Log("[LookBack]");
            foreach (string resource in lease.LeasedResources)
            {
                DebugClass.Log($"[LookBack] {resource}");
                // we look for the back to see if someone have the lease we need  
                for (int i = lease_index - 1; i >= 0; i--)
                {
                    DebugClass.Log($"[LookBack] {lease_index}");
                    if (leaseSheet[i].TmId != _transactionManager.Id
                            && leaseSheet[i].LeasedResources.Contains(resource))
                    {
                        DebugClass.Log($"[LookBack] added {resource}");
                        missingLeases.Add(resource);
                        break;
                    }
                }
            }
            return missingLeases;
        }

        public ClientTransactionReply executeOperations(ClientTransactionRequest request)
        {
            ClientTransactionReply reply = new ClientTransactionReply();
            // Execute reading operations.
            foreach (string readOp in request.ReadOperations)
            {
                if (!_transactionManager.DadInts.ContainsKey(readOp))
                {
                    reply.ObjValues.Add(null);
                }
                else
                {
                    reply.ObjValues.Add(_transactionManager.DadInts[readOp]);
                }
            }


            // Execute writting operations.
            foreach (DADInt dadInt in request.WriteOperations)
            {
                _transactionManager.DadInts[dadInt.Key] = dadInt.Value;
            }

            if (request.WriteOperations.ToList().Count() != 0)
            {

                // Propagate WriteOperations
                _transactionManager.URBroadCastMemory(request.WriteOperations.ToList());
            }
            return reply;
        }

        public static async Task<ReceiveLeaseResponse> RequestLs(
        LMTMCommunicationService.LMTMCommunicationServiceClient client,
        ReceiveLeaseRequest request)
        {
            return await client.ReceiveLeaseAsync(request);
        }

        public void RequestLeases(List<string> leases_to_request, int transactionId)
        {
            ReceiveLeaseRequest requestLease = new ReceiveLeaseRequest();
            Lease lease = new Lease();
            lease.TmId = _transactionManager.Id;
            lease.LeasedResources.AddRange(leases_to_request);
            lease.TransactionId = transactionId;
            requestLease.Lease = lease;

            var tasks = new List<Task<ReceiveLeaseResponse>>();

            foreach (var val in _transactionManager.LmsClients)
            {
                tasks.Add(RequestLs(val.Value, requestLease));
            }

            Task.WhenAll(tasks);
        }

    }
}

