using Grpc.Core;
using System.Linq;

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

        public ClientTransactionReply SubmitTransactionImpl(ClientTransactionRequest request)
        {


            _transactionManager.leasesMissing.Add("adw");

            ClientTransactionReply reply = new ClientTransactionReply();

            foreach (string readOp in request.ReadOperations)
            {

                if (_transactionManager.leaseList.Contains(readOp))    
                {
                    _transactionManager.leasesMissing.Add(readOp);
                }

            }

            foreach (DADInt dadInt in request.WriteOperations)
            {
                if (_transactionManager.leaseList.Contains(dadInt.Key))
                {
                    _transactionManager.leasesMissing.Add(dadInt.Key);
                }
            }

            //send lms for the lease sheet
            RequestLeases();



            return reply;
        }


        public void RequestLeases()
        {
            RequestLeaseRequest requestLease = new RequestLeaseRequest();
            requestLease.Leases.AddRange(_transactionManager.leasesMissing);

            foreach (var paxos in _transactionManager._lmsClients)
            {
                paxos.Value.RequestLease(requestLease);
            }
        }

    }
}

