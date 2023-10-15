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
                    Console.WriteLine("hmdwdmm");
                    _transactionManager.LeasesMissing.Add(readOp);
                    Console.WriteLine("~depois de aedicionar para um read", _transactionManager.LeasesMissing.Count);
                    Console.WriteLine(_transactionManager.LeasesMissing.Count());
                }

            }

            foreach (DADInt dadInt in request.WriteOperations)
            {
                if (!_transactionManager.LeaseList.Contains(dadInt.Key))
                {
                    _transactionManager.LeasesMissing.Add(dadInt.Key);
                    Console.WriteLine("~depois de aedicionar para um writed");
                    Console.WriteLine(_transactionManager.LeasesMissing.Count());

                }
            }

            //send lms for the lease sheet but check if its down
            if (!_transactionManager.RoundsDowns.Contains(_transactionManager.TimeSlot))
            {
                Console.WriteLine("antes de verificar", _transactionManager.LeasesMissing.Count);
                Console.WriteLine(_transactionManager.LeasesMissing.Count());
                if (_transactionManager.LeasesMissing.Count != 0)
                {
                    _transactionManager.TMLMService.RequestLeases();
                    //receive leasesheet
                    _transactionManager.Signal.Wait();
                    _transactionManager.Signal.Reset();
                }

            }

            foreach (LeaseSheet leaseSheet in _transactionManager.LeaseSheets)
            {
                if (leaseSheet.GetTmID() == _transactionManager.Id)
                {
                    if (leaseSheet.GetOrder() == 1) 
                    {
                        reply = executeOperations(request);
                    }
                    else
                    {
                        lookBackLeases(leaseSheet);
                        if (_transactionManager.LeasesMissing.Count != 0) { 
                            _transactionManager.TransactionManagerSignals[_transactionManager.Id].Wait();
                            _transactionManager.TransactionManagerSignals[_transactionManager.Id].Reset();
                        }

                        reply = executeOperations(request);
                    }

                    Dictionary<string, List<string>> leasesToSend = lookAheadLeases(leaseSheet);
                    foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                    {
                        //first check if the tm is down in this moment
                        if( !_transactionManager.RoundsDowns.Contains(_transactionManager.TimeSlot))
                        {
                            //im checking the suspicion list inside this
                            //here you ask for leases but dont send the request if you suspect the one you are asking
                            _transactionManager.CrossTMClientService.PropagateLease(leases.Key, leases.Value);

                        }

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
            int flag = 0;
            foreach (string readOp in request.ReadOperations)
            {

                foreach (DADInt memDadInt in _transactionManager.DadInts)
                {

                    if (memDadInt.Key == readOp)
                    {
                        Console.WriteLine(memDadInt.Value);
                        reply.ObjValues.Add(memDadInt.Value); //add value to reply
                    }
                }
            }

            foreach (DADInt dadInt in request.WriteOperations)
            {
                if(_transactionManager.DadInts.Count == 0)
                {
                    _transactionManager.DadInts.Add(dadInt);
                }
                else
                {
                    foreach (DADInt memDadInt in _transactionManager.DadInts)
                    {
                        if (memDadInt.Key == dadInt.Key)
                        {
                            flag = 1;
                            memDadInt.Value = dadInt.Value; //write value
                        }
                    }
                    if (flag == 0)
                    {
                        _transactionManager.DadInts.Add(dadInt);
                    }
                }


            }
            return reply;
        }

    }
}

