using Grpc.Net.Client;

namespace DADTKV.client
{
    class Client
    {
        private string _id = "";

        public string Id { get { return _id; } set { _id = value; } }

        private string _script = "";

        public string Script { get { return _script; } set { _script = value; } }

        private List<string> _tms = new List<string>();

        public List<string> Tms { get { return _tms; } set { _tms = value; } }

        private List<Command> _commands;

        public List<Command> Commands { get { return _commands; } set { _commands = value; } }

        public Client() {}

        private List<ClientServerService.ClientServerServiceClient> _tmsChannels
            = new List<ClientServerService.ClientServerServiceClient>();

        public List<ClientServerService.ClientServerServiceClient> TmsChannels { get { return _tmsChannels; } }



        public void createConnectionsToTms()
        {
            // Create connections to other Transmissions Managers
            foreach (var tm in Tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm);
                var tmChannel = new ClientServerService.ClientServerServiceClient(channel);
                TmsChannels.Add(tmChannel);
            }
        }

        public void ParseAndExecuteCommands(ClientService clientServer)
        {
            try
            {
                Commands = ScriptParser.ParseScript(Script);


                foreach (Command command in Commands)
                {
                    switch (command)
                    {
                        case TCommand:
                            TCommand tCommand = (TCommand)command;
                            clientServer.SubmitTransaction(tCommand.GetReadSet(), tCommand.GetWriteSet());
                            break;
                        case SCommand:
                            SCommand sCommand = (SCommand)command;
                            clientServer.Status();
                            break;
                        case WCommand:
                            WCommand wCommand = (WCommand)command;
                            Thread.Sleep(wCommand.GetWaitTime());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught Exception: {ex.Message}");
            }

            if (Commands != null)
            {
                foreach (Command command in Commands)
                {
                    //command.Print();
                }
            }
        }

        public void Start()
        {
            Console.WriteLine("Started client: " + Id);

            ClientService clientServer = new ClientService(this);

            createConnectionsToTms();

           
            ParseAndExecuteCommands(clientServer);
            while (true)
            {

            }
            
        }
    }
}
