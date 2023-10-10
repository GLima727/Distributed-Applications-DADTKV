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

            //receive leasesheet

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

        //public Dictionary<string, List<string>> lookAheadLeases(LeaseSheet leaseSheet)
        //{
        //    Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();
        //    foreach (string lease in leaseSheet.GetLeases())
        //    {
        //        for (int i = leaseSheet.GetOrder() + 1; i < leaseSheet.leases.Count; i++)
        //        {

        //            if ( != selfTmId && TransactionManager.leaseSheets[i].GetLeases().Contains(lease))
        //            {
        //                if (leasesToSend.ContainsKey(TransactionManager.leaseSheets[i].GetTmID()))
        //                {
        //                    leasesToSend[TransactionManager.leaseSheets[i].GetTmID()].Add(lease);
        //                }

        //                break;
        //            }
        //        }

        //    }
        //    return leasesToSend;
        //}

    }
}

