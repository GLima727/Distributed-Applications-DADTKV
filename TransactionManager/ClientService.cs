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


            ClientTransactionReply reply = new ClientTransactionReply();

            foreach (string readOp in request.ReadOperations)
            {

                if (!_transactionManager.LeaseList.Contains(readOp))    
                {
                    _transactionManager.LeasesMissing.Add(readOp);
                }

            }

            foreach (DADInt dadInt in request.WriteOperations)
            {
                if (!_transactionManager.LeaseList.Contains(dadInt.Key))
                {
                    _transactionManager.LeasesMissing.Add(dadInt.Key);
                }
            }

            //send lms for the lease sheet
            _transactionManager.TMLMService.RequestLeases();

            //receive leasesheet
            _transactionManager.Signal.Wait();
            _transactionManager.Signal.Reset();

            foreach (LeaseSheet leaseSheet in _transactionManager.LeaseSheets)
            {
                if (leaseSheet.GetTmID() == _transactionManager.Id)
                {
                    if (leaseSheet.GetOrder() == 1) //if it sees its own leases
                    {
                        //execute transaction
                        reply = executeOperations(request);
                    }
                    else
                    {
                        //lookback
                        lookBackLeases(leaseSheet);
                        _transactionManager.TransactionManagerSignals[_transactionManager.Id].Wait();
                        _transactionManager.TransactionManagerSignals[_transactionManager.Id].Reset();

                        //execute transaction
                        reply = executeOperations(request);
                    }
                    //lookahead

                    Dictionary<string, List<string>> leasesToSend = lookAheadLeases(leaseSheet);
                    foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                    {
                        //check if tm suspects tm he is sending to
                        foreach(var suspicion in _transactionManager.SusList)
                        {
                            //if the suspicion is in the current time check for id
                            if(suspicion.Item1 == _transactionManager.TimeSlot)
                            {

                            }
                        }
                        _transactionManager.CrossTMClientService.PropagateLease(leases.Key, leases.Value);

                        // remove A from ("A","B") and so on
                        foreach (string lease in leases.Value)
                        {
                            _transactionManager.LeaseList.Remove(lease);

                        }
                    }

                }
            }

            return reply;
        }

        public Dictionary<string, List<string>> lookAheadLeases(LeaseSheet currentLease)
        {
            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();
            foreach (string lease in currentLease.GetLeases())
            {
                for (int i = currentLease.GetOrder() + 1; i < _transactionManager.LeaseSheets.Count; i++)
                {

                    if ( _transactionManager.LeaseSheets[i].GetTmID() != _transactionManager.Id && _transactionManager.LeaseSheets[i].GetLeases().Contains(lease))
                    {
                        //atencao verificar se ele nao esta a criar ids repetidos no dicionario
                        leasesToSend[_transactionManager.LeaseSheets[i].GetTmID()].Add(lease);
                        break;
                    }
                }

            }
            return leasesToSend;
        }

        public void lookBackLeases(LeaseSheet leaseSheet)
        {
            foreach (string lease in leaseSheet.GetLeases())
            {
                List<string> leases = new List<string>();
                for (int i = leaseSheet.GetOrder() - 1; i >= 0; i--)
                {
                    if (_transactionManager.LeaseSheets[i].GetTmID() != _transactionManager.Id && _transactionManager.LeaseSheets[i].GetLeases().Contains(lease))
                    {
                        leases.Add(lease);
                        break;
                    }
                }
                _transactionManager.LeasesMissing = leases;
            }
        }

        public ClientTransactionReply executeOperations(ClientTransactionRequest request)
        {
            ClientTransactionReply reply = new ClientTransactionReply();
            foreach (string readOp in request.ReadOperations)
            {
                foreach (DADInt memDadInt in _transactionManager.DadInts)
                {
                    if (memDadInt.Key == readOp)
                    {
                        reply.ObjValues.Add(memDadInt.Value); //add value to reply
                    }
                }
            }

            foreach (DADInt dadInt in request.WriteOperations)
            {
                foreach (DADInt memDadInt in _transactionManager.DadInts)
                {
                    if (memDadInt.Key == dadInt.Key)
                    {
                        memDadInt.Value = dadInt.Value; //write value
                    }
                }
            }
            return reply;
        }

    }
}

