using Grpc.Core;

namespace DADTKV.transactionManager
{
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

            ManualResetEventSlim processSignal = new ManualResetEventSlim(false);
            _transactionManager.TransactionQueue.Enqueue(processSignal);

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
                    _transactionManager.AddMissingLease(readOp);
                }
            }

            DebugClass.Log("[SubmitTransactionImpl] [Write transactions]");
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] Needs to access {dadInt.Key}.");
                if (!_transactionManager.ContainsLease(dadInt.Key))
                {
                    _transactionManager.AddMissingLease(dadInt.Key);
                    DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] doesn't have {dadInt.Key}.");
                }
            }


            DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] missing {_transactionManager.LeasesMissing.Count} leases");
            if (_transactionManager.LeasesMissing.Count != 0)
            {
                RequestLeases(_transactionManager.LeasesMissing, currentTransId);

                _transactionManager.NumberLms = 0;
                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Sent lease requests.");
                // Wait to receive lease sheet
                processSignal.Wait();
                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Received lease sheet.");
                processSignal.Reset();
                // send lms for the lease sheet but check if its down

                int lease_index = 0;

                // For each Lease i received
                foreach (Lease lease in _transactionManager.LeaseSheet)
                {
                    // Check if this lease is for this tmId
                    if (lease.TmId == _transactionManager.Id && lease.TransactionId == currentTransId)
                    {
                        // if is the first dont look back
                        if (lease_index == 0 && _transactionManager.NRound == 1)
                        {
                            _transactionManager.LeasesAvailable = _transactionManager.LeasesMissing;
                            _transactionManager.LeasesMissing = new List<string>();
                            DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] I was the first to receive this lease.");
                        }
                        else
                        {
                            // check if someone have the leases we need
                            _transactionManager.LeasesMissing = lookBackLeases(lease, lease_index);
                            foreach (var l in _transactionManager.LeasesMissing)
                            {
                                DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] missing {l}");
                            }

                            if (_transactionManager.LeasesMissing.Count != 0)
                            {
                                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] we need to wait to get leases from others tms");
                                // Wait for others tm to give leases
                                _transactionManager.TransactionManagerSignal.Wait();
                                _transactionManager.TransactionManagerSignal.Reset();
                            }
                        }

                        reply = executeOperations(request);

                        // Propagate WriteOperations
                        _transactionManager.URBroadCastMemory(request.WriteOperations.ToList());

                        // Send Leases to anyone who needs it
                        DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases]");
                        Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index);
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

                // Propagate WriteOperations
                _transactionManager.URBroadCastMemory(request.WriteOperations.ToList());
            }

            DebugClass.Log($"[SubmitTransactionImpl] pass to the next in queue");
            if (_transactionManager.TransactionQueue.Count > 0)
            {
                _transactionManager.TransactionQueue.Dequeue().Set();
            }

            return reply;
        }

        public Dictionary<string, List<string>> lookAheadLeases(Lease lease, int lease_index)
        {
            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();
            foreach (string resource in lease.LeasedResources)
            {
                for (int i = lease_index + 1; i < _transactionManager.LeaseSheet.Count; i++)
                {
                    if (_transactionManager.LeaseSheet[i].LeasedResources.Contains(resource))
                    {
                        if (_transactionManager.LeaseSheet[i].TmId != _transactionManager.Id)
                        {
                            // atencao verificar se ele nao esta a criar ids repetidos no dicionario
                            if (!leasesToSend.ContainsKey(_transactionManager.LeaseSheet[i].TmId))
                            {
                                leasesToSend[_transactionManager.LeaseSheet[i].TmId] = new List<string>();
                            }
                            leasesToSend[_transactionManager.LeaseSheet[i].TmId].Add(resource);
                        }
                        break;
                    }
                }

            }

            return leasesToSend;
        }

        public List<string> lookBackLeases(Lease lease, int lease_index)
        {
            List<string> missingLeases = new List<string>();
            // for each resource we want
            foreach (string resource in lease.LeasedResources)
            {
                // we look for the back to see if someone have the lease we need  
                for (int i = lease_index - 1; i >= 0; i--)
                {
                    if (_transactionManager.LeaseSheet[i].TmId != _transactionManager.Id
                            && _transactionManager.LeaseSheet[i].LeasedResources.Contains(resource))
                    {
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

