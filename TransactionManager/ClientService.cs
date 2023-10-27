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
            get { return _transId; }
            set { _transId = value; }
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
            return _transactionManager.RoundsDowns.Contains(_transactionManager.CurrentRound);
        }

        public ClientTransactionReply SubmitTransactionImpl(ClientTransactionRequest request)
        {
            bool done = false;
            ClientTransactionReply reply = new ClientTransactionReply();

            while (!done)
            {
                DebugClass.Log("[SubmitTransactionImpl] Received transaction.");
                TransactionInfo transaction = new TransactionInfo();
                transaction.TransactionID = TransactionID++;
                transaction.TransactionRequest = request;


                if (TmIsDown())
                {
                    DebugClass.Log("[SubmitTransactionImpl] This TM is down.");
                    DADInt dADInt = new DADInt();
                    dADInt.Key = "ABORT";
                    dADInt.Value = -1;
                    reply.ObjValues.Add(dADInt);
                    return reply;
                }

                Monitor.Enter(_transactionManager.TMLock);
                foreach (string readOp in request.ReadOperations)
                {
                    DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] Needs to access {readOp}.");
                    if (!_transactionManager.LeasesAvailable.Contains(readOp))
                    {
                        DebugClass.Log($"[SubmitTransactionImpl] [Read transactions] doesn't have {readOp}.");
                        transaction.MissingLeases.Add(readOp);
                    }
                }

                foreach (DADInt dadInt in request.WriteOperations)
                {
                    DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] Needs to access {dadInt.Key}.");
                    if (!_transactionManager.LeasesAvailable.Contains(dadInt.Key))
                    {
                        DebugClass.Log($"[SubmitTransactionImpl] [Write transactions] doesn't have {dadInt.Key}.");
                        transaction.MissingLeases.Add(dadInt.Key);
                    }
                }
                Monitor.Exit(_transactionManager.TMLock);

                int current_round_paxos = _transactionManager.CurrentRoundPaxos;

                DebugClass.Log($"[SubmitTransactionImpl] missing {transaction.MissingLeases.Count}.");
                if (transaction.MissingLeases.Count == 0)
                {
                    DebugClass.Log($"[SubmitTransactionImpl] have lease.");
                    reply = _transactionManager.executeOperations(request);
                    done = true;
                }
                else
                {

                    Monitor.Enter(_transactionManager.LMTMLock);
                    DebugClass.Log($"[SubmitTransactionImpl] doesn't have lease in round {current_round_paxos}.");
                    if (_transactionManager.CurrentRoundPaxos != 0)
                    {
                        _transactionManager.TransactionEpochList[current_round_paxos - 1].EpochSignal.Wait();
                    }

                    _transactionManager.TransactionEpochList[current_round_paxos].TransactionQueue.Add(transaction);
                    RequestLeases(transaction.MissingLeases, transaction.TransactionID);
                    Monitor.Exit(_transactionManager.LMTMLock);
                    transaction.SignalClient.Wait();

                    if (transaction.status == -1)
                    {
                        done = false;
                    } else {
                        done = true;
                        reply = transaction.TransactionReply;
                    }
                }
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

