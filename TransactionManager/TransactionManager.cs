using Grpc.Core;
using Grpc.Net.Client;

namespace DADTKV.transactionManager
{
    class TransactionManager
    {
        private string _id = "";
        public string Id { get { return _id; } set { _id = value; }}

        private string _url = "";
        public string Url { get { return _url; } set { _url = value; } }

        private int _port = 0; 

        public int Port { get { return _port; } set { _port = value; } }

        private int _timeSlot = 0;
        public int TimeSlot { get { return _timeSlot; } set { _timeSlot = value; } }

        private int _numSlot = 0;
        public int NumSlot { get { return _numSlot; } set { _numSlot = value; } }

        private List<int> _roundsDowns = new List<int>();
        public List<int> RoundsDowns { get { return _roundsDowns; } set { _roundsDowns = value; } }

        private string _timeStart = "";
        public string TimeStart { get { return _timeStart; } set { _timeStart = value; } }

        private List<Tuple<string, string>> _tms = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> Tms { get { return _tms; } set { _tms = value; } }

        private List<Tuple<string, string>> _lms = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> Lms { get { return _lms; } set { _lms = value; } }

        private List<Tuple<int, string>> _susList = new List<Tuple<int, string>>();
        public List<Tuple<int, string>> SusList { get { return _susList; } set { _susList = value; } }

        public List<string> leasesMissing = new List<string>();

        public List<string> leaseList = new List<string>();

        public List<LeaseSheet> leaseSheets = new List<LeaseSheet>();

        public ManualResetEventSlim signal = new ManualResetEventSlim(false);

        public Dictionary<string, ManualResetEventSlim> transactionManagerSignals = new Dictionary<string, ManualResetEventSlim>();

        public List<DADInt> dadInts = new List<DADInt>();


        //LM ID, Client
        public Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient> _lmsClients
            = new Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient>();

        //TM ID, Client
        public Dictionary<string, CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient> _tmsClients
            = new Dictionary<string, CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient>();

        public CrossTMClientService crossTmClientService;
        public void createConnectionsToLms()
            {
                // Create connections to other Transmissions Managers
                foreach (var lm in _lms)
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(lm.Item2);
                    var client = new PaxosCommunicationService.PaxosCommunicationServiceClient(channel);
                    _lmsClients.Add(lm.Item1, client);
                }
            }

        public void createConnectionsToTms()
        {
            // Create connections to other Transmissions Managers
            foreach (var tm in _tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm.Item2);
                var client = new CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient(channel);
                _tmsClients.Add(tm.Item1, client);
            }
        }

        public void WaitForStartTime()
        {
            DateTime startTime = DateTime.ParseExact(TimeStart, "HH:mm:ss", null);

            while (DateTime.Now < startTime)
            {
                // Wait for a short period of time before checking again
                System.Threading.Thread.Sleep(1000); // Sleep for 1 second (adjust as needed)
            }
        }

        public void Start()
        {
            // Create Server
            crossTmClientService = new CrossTMClientService(this);

            ServerPort serverPort = new ServerPort(_url, _port, ServerCredentials.Insecure);
            Server server = new Server
            {
                Services = {
                    CrossServerTransactionManagerService.BindService(new CrossTMServerService(this)),
                    ClientServerService.BindService(new ClientService(this)),
                    LMTMCommunicationService.BindService(new LeaseManagerServicings(this)),
                },
                Ports = { serverPort }
            };

            server.Start();

            createConnectionsToLms();
            createConnectionsToTms();

            while (true) ;
        }
    }
}
