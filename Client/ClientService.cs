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
            return _client.TmsChannels[number];
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

            ClientTransactionReply reply = GetTransactionManager().SubmitTransaction(request);

            foreach (int value in reply.ObjValues)
            {
                Console.WriteLine(value);
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
