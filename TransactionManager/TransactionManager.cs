using Grpc.Core;
using Grpc.Net.Client;

namespace DADTKV.transactionManager
{
    class TransactionManager
    {
        private string _id = "";
        public string Id { get { return _id; } set { _id = value; } }

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

        private List<string> _leasesMissing = new List<string>();

        public List<string> LeasesMissing { get { return _leasesMissing; } set { _leasesMissing = value; } }

        private List<string> _leaseList = new List<string>();

        public List<string> LeaseList { get { return _leaseList; } set { _leaseList = value; } }

        private List<LeaseSheet> _leaseSheets = new List<LeaseSheet>();
        public List<LeaseSheet> LeaseSheets { get { return _leaseSheets; } set { _leaseSheets = value; } }

        private ManualResetEventSlim _signal = new ManualResetEventSlim(false);
        public ManualResetEventSlim Signal { get { return _signal; } }

        private Dictionary<string, ManualResetEventSlim> _transactionManagerSignals = new Dictionary<string, ManualResetEventSlim>();

        public Dictionary<string, ManualResetEventSlim> TransactionManagerSignals { get { return _transactionManagerSignals; } }

        private List<DADInt> _dadInts = new List<DADInt>();
        public List<DADInt> DadInts { get { return _dadInts; } set { _dadInts = value; } }

        //LM ID, Client
        private Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient> _lmsClients
            = new Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient>();

        public Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient> LmsClients { get { return _lmsClients; } set { _lmsClients = value; } }

        //TM ID, Client
        private Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> _tmsClients
            = new Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>>();

        public Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> TmsClients { get { return _tmsClients; } set { _tmsClients = value; } }

        private CrossTMClientService _crossTmClientService;

        public CrossTMClientService CrossTMClientService { get { return _crossTmClientService; } set { _crossTmClientService = value; } }

        private TMLMService _tMLMService;

        public TMLMService TMLMService { get { return _tMLMService; } set { _tMLMService = value; } }
        public void createConnectionsToLms()
        {
            // Create connections to other Transmissions Managers
            foreach (var lm in Lms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(lm.Item2);
                var client = new LMTMCommunicationService.LMTMCommunicationServiceClient(channel);
                LmsClients.Add(lm.Item1, client);
            }
        }

        public void createConnectionsToTms()
        {
            // Create connections to other Transmissions Managers
            foreach (var tm in Tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm.Item2);
                var client = new CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient(channel);

                Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>> tuple 
                    = new Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>> (client, RoundsDowns);

                TmsClients.Add(tm.Item1, tuple);
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
            DebugClass.Log($"Start Transaction Manager {_id}.");

            // Create Server
            CrossTMClientService = new CrossTMClientService(this);
            TMLMService = new TMLMService(this);

            ServerPort serverPort = new ServerPort(_url, _port, ServerCredentials.Insecure);
            Server server = new Server
            {
                Services = {
                    CrossServerTransactionManagerService.BindService(new CrossTMServerService(this)),
                    ClientServerService.BindService(new ClientService(this)),
                    LMTMCommunicationService.BindService(new LMTMService(this)),
                },
                Ports = { serverPort }
            };

            server.Start();
            DebugClass.Log("Set connections to another LMs.");
            createConnectionsToLms();
            DebugClass.Log("Set connections to TMs.");
            createConnectionsToTms();

            DebugClass.Log("Waiting for wall time.");
            WaitForStartTime();
            DebugClass.Log("Wall time completed.");


            // Simulate Connections
            // Send 4 leases requests
            for (int i = 0; i < 4; i++)
            {
                foreach (var lm in _lmsClients)
                {
                    var req = new LeaseRequest();
                    var lease = new Lease();
                    lease.TmId = _id;
                    lease.Leases.Add("ola");
                    req.LeaseDetails = lease;
                    lm.Value.ProcessLeaseRequest(req);
                }
            }

            while (true) ;
        }
    }
}
