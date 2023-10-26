using Grpc.Core;

namespace DADTKV.transactionManager
{
    class ClientService : ClientServerService.ClientServerServiceBase
    {
        private TransactionManager _transactionManager;

        private int _transId = 0;
        private object _transIdLock = new object();

        public int TransactionID
        {
            get { lock (_transIdLock) { return _transId; } }
            set { lock (_transIdLock) { _transId = value; } }
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
            TransactionInfo transaction = new TransactionInfo();
            transaction.TransactionID = _transId++;
            transaction.Info = request;


            ClientTransactionReply reply = new ClientTransactionReply();
            if (TmIsDown())
            {
                DebugClass.Log("[SubmitTransactionImpl] This TM is down.");
                DADInt dADInt = new DADInt();
                dADInt.Key = "ABORT";
                dADInt.Value = -1;
                reply.ObjValues.Add(dADInt);
                return reply;
            }

            DebugClass.Log("[SubmitTransactionImpl] [Read transactions]");
            foreach (string readOp in request.ReadOperations)
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] Needs to access {readOp}.");
                if (!_transactionManager.ContainsLease(readOp))
                {
                    DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] doesn't have {readOp}.");
                    transaction.MissingLeases.Add(readOp);
                }
            }

            DebugClass.Log("[SubmitTransactionImpl] [Write transactions]");
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] Needs to access {dadInt.Key}.");
                if (!_transactionManager.ContainsLease(dadInt.Key))
                {
                    DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] doesn't have {dadInt.Key}.");
                    transaction.MissingLeases.Add(dadInt.Key);
                }
            }

            if (transaction.MissingLeases.Count == 0)
            {
                reply = executeOperations(request);
            }
            else
            {
                //_transactionManager.TransactionEpochList[_transactionManager.TimeSlot - 1].Add(transaction);
                 if (_transactionManager.TimeSlot != 0)
                {
                    //_transactionManager.TransactionEpochList[_transactionManager.TimeSlot - 1].
                }
                RequestLeases(transaction.MissingLeases,transaction.TransactionID);
                DADInt dADInt = new DADInt();
                dADInt.Key = "OK";
                dADInt.Value = -1;
                reply.ObjValues.Add(dADInt);
                
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
                        // atencao verificar se ele nao esta a criar ids repetidos no dicionario
                        if (!leasesToSend.ContainsKey(leaseSheet[i].TmId))
                        {
                            leasesToSend[leaseSheet[i].TmId] = new List<string>();
                        }
                        leasesToSend[leaseSheet[i].TmId].Add(resource);

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
                    if (leaseSheet[i].LeasedResources.Contains(resource))
                    {
                        if (leaseSheet[i].TmId != _transactionManager.Id)
                        {
                            DebugClass.Log($"[LookBack] added {resource}");
                            missingLeases.Add(resource);
                        }
                        else
                        {
                            DebugClass.Log($"[LookBack] we have the lease {resource}");
                        }
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
                    reply.ObjValues.AddRange(null);
                }
                else
                {
                    DADInt dadInt = new DADInt();
                    dadInt.Key = readOp;
                    dadInt.Value = _transactionManager.DadInts[readOp];
                    reply.ObjValues.Add(dadInt);
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

