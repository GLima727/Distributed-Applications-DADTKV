using Grpc.Net.Client;

namespace DADTKV.client
{
    public class ClientService
    {
        private List<ClientServerService.ClientServerServiceClient> transactionManagerServer = new List<ClientServerService.ClientServerServiceClient>();
        private ClientServerService.ClientServerServiceClient? server = null;
        private readonly string clientId;

        /// <summary>
        /// ClientService Constructor's.
        /// </summary>
        public ClientService(List<string> transationManAdrr, string id)
        {

            this.clientId = id;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            foreach (string addr in transationManAdrr)
            {
                Console.WriteLine(addr);
                GrpcChannel thisChannel = GrpcChannel.ForAddress(addr);

                server = new ClientServerService.ClientServerServiceClient(thisChannel);

                transactionManagerServer.Add(server);
            }
        }

        /// <summary>
        /// Method to create a sort of LoadBalancer for the TMs.
        /// </summary>
        public ClientServerService.ClientServerServiceClient GetTransactionManager()
        {
            Random rnd = new Random();
            int number = rnd.Next(0, transactionManagerServer.Count - 1);
            return transactionManagerServer[number];
        }

        /// <summary>
        /// gRPC's method for submitting a transaction.
        /// </summary>
        public void SubmitTransaction(List<string> readSet, List<(string, int)> writeSet)
        {


            ClientTransactionRequest request = new ClientTransactionRequest();
            request.ClientId = this.clientId;
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

            foreach (ClientServerService.ClientServerServiceClient tm in this.transactionManagerServer)
            {
                tm.Status(statusRequest);
            }
        }

    }
}
