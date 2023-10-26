namespace DADTKV.client
{
    class ClientService
    {
        private Client _client;

        /// <summary>
        /// ClientService Constructor's.
        /// </summary>
        public ClientService(Client client)
        {
            _client = client;
        }

        /// <summary>
        /// Method to create a sort of LoadBalancer for the TMs.
        /// </summary>
        public ClientServerService.ClientServerServiceClient GetTransactionManager()
        {
            Random rnd = new Random();
            int number = rnd.Next(0, _client.TmsChannels.Count);
            if (_client.Id == "client1")
                return _client.TmsChannels[0];
            //else if (_client.Id == "client2")
            //    return _client.TmsChannels[1];
            //else
            return _client.TmsChannels[1];
        }

        /// <summary>
        /// gRPC's method for submitting a transaction.
        /// </summary>
        public void SubmitTransaction(List<string> readSet, List<(string, int)> writeSet)
        {
            ClientTransactionRequest request = new ClientTransactionRequest();
            request.ClientId = _client.Id;
            request.ReadOperations.Add(readSet);

            foreach ((string, int) pair in writeSet)
            {
                DADInt dADInt = new DADInt
                {
                    Value = pair.Item2,
                    Key = pair.Item1
                };
                request.WriteOperations.Add(dADInt);
            }

            var tm = GetTransactionManager();
            var reply = tm.SubmitTransaction(request);

            if (reply.ObjValues.Count == 1 && reply.ObjValues[0].Key == "OK" && reply.ObjValues[0].Value == -1)
            {
                DebugClass.Log("Transaction received.");
            }
            else if (reply.ObjValues.Count == 1 && reply.ObjValues[0].Key == "ABORT" && reply.ObjValues[0].Value == -1)
            {
                DebugClass.Log("Transaction aborted.");
            }
            else
            {
                foreach (DADInt value in reply.ObjValues)
                {
                    Console.WriteLine($"<{DADInt.ValueFieldNumber}, {DADInt.KeyFieldNumber}>");
                }
            }
        }

        /// <summary>
        /// gRPC's method for requesting every TM to print their status.
        /// </summary>
        public void Status()
        {
            ClientStatusRequest statusRequest = new ClientStatusRequest();

            foreach (ClientServerService.ClientServerServiceClient tm in _client.TmsChannels)
            {
                tm.Status(statusRequest);
            }
        }

    }
}
