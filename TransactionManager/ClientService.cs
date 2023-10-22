using Grpc.Core;

namespace DADTKV.transactionManager
{
    class ClientService : ClientServerService.ClientServerServiceBase
    {
        private TransactionManager _transactionManager;

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
            DebugClass.Log("Received transaction.");
            ClientTransactionReply reply = new ClientTransactionReply();
            if (TmIsDown())
            {
                return reply;
            }

            DebugClass.Log("Check for read transactions.");
            foreach (string readOp in request.ReadOperations)
            {
                DebugClass.Log($"-----Received lease {readOp}.");
                if (!_transactionManager.ContainsLease(readOp))
                {
                    DebugClass.Log($"-----Doesn't have lease {readOp}.");
                    _transactionManager.AddMissingLease(readOp);
                }

            }

            DebugClass.Log("Check for write transactions.");
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DebugClass.Log($"----Received lease {dadInt.Key}.");
                if (!_transactionManager.ContainsLease(dadInt.Key))
                {
                    _transactionManager.AddMissingLease(dadInt.Key);
                    DebugClass.Log($"----Doesn't have lease {dadInt.Key}.");
                }
            }

            if (_transactionManager.LeasesMissing.Count != 0)
            {
                RequestLeases();
                _transactionManager.NumberLms = 0;
                DebugClass.Log("Sent lease requests.");
                // Wait to receive lease sheet
                _transactionManager.Signal.Wait();
                DebugClass.Log("Received lease sheet.");
                _transactionManager.Signal.Reset();
            }

            // send lms for the lease sheet but check if its down
            int lease_index = 0;

            // For each Lease i received
            foreach (Lease lease in _transactionManager.LeaseSheet)
            {
                // Check if this lease is for this tmId
                if (lease.TmId == _transactionManager.Id)
                {
                    DebugClass.Log($"-----Received Lease {string.Join("-", lease.LeasedResources.ToList())}.");
                    // if is the first dont look back
                    if (lease_index == 0)
                    {
                        DebugClass.Log($"-----I am the first to receive this Lease");
                        reply = executeOperations(request);
                        _transactionManager.LeaseList = _transactionManager.LeasesMissing;
                        _transactionManager.LeasesMissing = new List<string>();
                    }
                    else
                    {
                        DebugClass.Log($"-----Check if I am missing leases");
                        lookBackLeases(lease, lease_index);
                        if (_transactionManager.LeasesMissing.Count != 0)
                        {
                            // tirar array  
                            _transactionManager.CrossTransactionManagerSignal.Wait();
                            _transactionManager.CrossTransactionManagerSignal.Reset();
                        }
                        reply = executeOperations(request);
                    }

                    // Send Leases to anyone who needs it
                    Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index);
                    foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                    {
                        //im checking the suspicion list inside this
                        //here you ask for leases but dont send the request if you suspect the one you are asking
                        PropagateLease(leases.Key, leases.Value);

                        // remove A from ("A","B") and so on
                        foreach (string resource in leases.Value)
                        {
                            _transactionManager.RemoveLeaseToList(resource);
                        }
                    }
                    lease_index++;
                }
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

                    if (_transactionManager.LeaseSheet[i].TmId != _transactionManager.Id
                            && _transactionManager.LeaseSheet[i].TmId.Contains(resource))
                    {
                        // atencao verificar se ele nao esta a criar ids repetidos no dicionario
                        leasesToSend[_transactionManager.LeaseSheet[i].TmId].Add(resource);
                        break;
                    }
                }

            }
            return leasesToSend;
        }

        public void lookBackLeases(Lease lease, int lease_index)
        {
            foreach (string resource in lease.LeasedResources)
            {
                List<string> leases = new List<string>();
                for (int i = lease_index - 1; i >= 0; i--)
                {
                    if (_transactionManager.LeaseSheet[i].TmId != _transactionManager.Id
                            && _transactionManager.LeaseSheet[i].LeasedResources.Contains(resource))
                    {
                        leases.Add(resource);
                        break;
                    }
                }
                _transactionManager.LeasesMissing = leases;
            }
        }

        public ClientTransactionReply executeOperations(ClientTransactionRequest request)
        {
            DebugClass.Log("Execute reading operations");
            ClientTransactionReply reply = new ClientTransactionReply();
            // Execute reading operations.
            foreach (string readOp in request.ReadOperations)
            {
                if (!_transactionManager.DadInts.ContainsKey(readOp))
                {
                    DebugClass.Log($"Reading {readOp} - NULL");
                    reply.ObjValues.Add(null);
                }
                else
                {
                    DebugClass.Log($"Reading {readOp} - {_transactionManager.DadInts[readOp]}");
                    reply.ObjValues.Add(_transactionManager.DadInts[readOp]);
                }
            }

            // Execute writting operations.
            DebugClass.Log("Execute write operations");
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DebugClass.Log($"Write {dadInt.Key} -  {dadInt.Value}.");
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

        public void RequestLeases()
        {
            ReceiveLeaseRequest requestLease = new ReceiveLeaseRequest();
            Lease lease = new Lease();
            lease.TmId = _transactionManager.Id;
            lease.LeasedResources.AddRange(_transactionManager.LeasesMissing);
            requestLease.Lease = lease;

            var tasks = new List<Task<ReceiveLeaseResponse>>();

            foreach (var val in _transactionManager.LmsClients)
            {
                tasks.Add(RequestLs(val.Value, requestLease));
            }

            Task.WhenAll(tasks);
        }

        public void PropagateLease(string tmID, List<string> leases)
        {
            PropagateLeasesRequest request = new PropagateLeasesRequest();
            request.Leases.AddRange(leases);

            //checks if any transaction manager can respond to it in this timeslot
            lock (_transactionManager)
            {
                if (!_transactionManager.TmsClients[tmID].Item2.Contains(_transactionManager.TimeSlot))
                {
                    //if you dont suspect the tm at this timeslot you can ask for the leases
                    _transactionManager.TmsClients[tmID].Item1.PropagateLeases(request);
                }
            }
        }
    }
}

