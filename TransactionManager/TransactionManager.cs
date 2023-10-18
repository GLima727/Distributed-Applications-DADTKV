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
        private object _leasesMissingLock = new object();
        public List<string> LeasesMissing
        {
            get { lock (_leasesMissingLock) { return _leasesMissing; } }
            set { lock (_leasesMissingLock) { _leasesMissing = value; } }
        }

        private List<string> _leaseList = new List<string>();
        private object _leaseListLock = new object();

        public List<string> LeaseList
        {
            get { lock (_leaseListLock) { return _leaseList; } }
            set { lock (_leaseListLock) { _leaseList = value; } }
        }

        private List<LeaseSheet> _leaseSheets = new List<LeaseSheet>();
        private object _leaseSheetsLock = new object();
        public List<LeaseSheet> LeaseSheets
        {
            get { lock (_leaseSheetsLock) { return _leaseSheets; } }
            set { lock (_leaseSheetsLock) { _leaseSheets = value; } }
        }

        private ManualResetEventSlim _signal = new ManualResetEventSlim(false);
        public ManualResetEventSlim Signal { get { return _signal; } }

        private Dictionary<string, ManualResetEventSlim> _transactionManagerSignals = new Dictionary<string, ManualResetEventSlim>();
        private object _transactionManagerSignalsLock = new object();
        public Dictionary<string, ManualResetEventSlim> TransactionManagerSignals
        {
            get { lock (_transactionManagerSignalsLock) { return _transactionManagerSignals; } }
        }

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

        public TransactionManager()
        {
            _crossTmClientService = new CrossTMClientService(this);
            _tMLMService = new TMLMService(this);
        }

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
            List<int> roundsSuspected;
            // Create connections to other Transmissions Managers
            foreach (var tm in Tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm.Item2);
                var client = new CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient(channel);

                roundsSuspected = new List<int>();

                foreach (Tuple<int, string> susList in SusList)
                {
                    //if the current tm suspects this tm in any rounds
                    if (susList.Item2 == tm.Item1)
                    {
                        //rounds where current tm will not talk with the clienttm
                        roundsSuspected.Add(susList.Item1);
                    }
                }
                TransactionManagerSignals.Add(tm.Item1, new ManualResetEventSlim(false));
                Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>> tuple
                    = new Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>(client, roundsSuspected);

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


            while (true) ;
        }

        public bool ContainsLease(string lease)
        {
            lock (_leaseListLock)
            {
                return _leaseList.Contains(lease);
            }
        }

        public void AddLeaseToList(string lease)
        {
            lock (_leaseListLock)
            {
                _leaseList.Add(lease);
            }
        }

        public void RemoveLeaseToList(string lease)
        {
            lock (_leaseListLock)
            {
                _leaseList.Remove(lease);
            }
        }

        public void AddMissingLease(string lease)
        {
            lock (_leasesMissingLock)
            {
                _leasesMissing.Add(lease);
            }
        }

        public void RemoveMissingLease(string lease)
        {
            lock (_leasesMissingLock)
            {
                _leasesMissing.Remove(lease);
            }
        }

        public void AddLeaseSheet(LeaseSheet leaseS)
        {
            lock (_leaseSheetsLock)
            {
                _leaseSheets.Add(leaseS);
            }
        }
    }
}
