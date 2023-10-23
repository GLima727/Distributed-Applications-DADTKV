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

        private int _propagateId = 0;
        public int PropagateId { get { return _propagateId; } set { _propagateId = value; } }

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

        private List<string> _leasesAvailable = new List<string>();
        private object _leaseListLock = new object();

        public List<string> LeasesAvailable
        {
            get { lock (_leaseListLock) { return _leasesAvailable; } }
            set { lock (_leaseListLock) { _leasesAvailable = value; } }
        }

        private List<(string, string)> _sendTaks = new List<(string, string)>();
        private object _sendTaksLock = new object();

        public List<(string, string)> SendTaks
        {
            get { lock (_sendTaksLock) { return _sendTaks; } }
            set { lock (_leaseListLock) { _sendTaks = value; } }
        }

        private List<Lease> _leaseSheet = new List<Lease>();
        private object _leaseSheetsLock = new object();
        public List<Lease> LeaseSheet
        {
            get { lock (_leaseSheetsLock) { return _leaseSheet; } }
            set { lock (_leaseSheetsLock) { _leaseSheet = value; } }
        }

        private List<Lease> _propagateLeasesNeed = new List<Lease>();
        public List<Lease> PropagateLeasesNeed { get { return _propagateLeasesNeed; } set { _propagateLeasesNeed = value; } }

        private int _numberLms;

        public int NumberLms { get { return _numberLms; } set { _numberLms = value; } }

        private ManualResetEventSlim _signal = new ManualResetEventSlim(false);
        public ManualResetEventSlim Signal { get { return _signal; } }

        private ManualResetEventSlim _transactionManagerSignal = new ManualResetEventSlim();
        private object _transactionManagerSignalsLock = new object();
        public ManualResetEventSlim TransactionManagerSignal
        {
            get { lock (_transactionManagerSignalsLock) { return _transactionManagerSignal; } }
        }

        private Queue<ManualResetEventSlim> _transactionQueue = new Queue<ManualResetEventSlim>();
        private object _transactionQueueLock = new object();
        public Queue<ManualResetEventSlim> TransactionQueue
        {
            get { lock (_transactionQueueLock) { return _transactionQueue; } }
            set { lock (_transactionQueueLock) { _transactionQueue = value; } }
        }

        private int _transactionID = 0;
        private object _transactionIDLock = new object();

        public int TransactionID
        {
            get { lock (_transactionIDLock) { return _transactionID;  } }
            set { lock (_transactionIDLock) { _transactionID = value; } }
        }


        private Dictionary<string, int> _dadInts = new Dictionary<string, int>();
        private object _dadIntsLock = new object();
        public Dictionary<string, int> DadInts
        {
            get { lock (_dadIntsLock) { return _dadInts; } }
            set { lock (_dadIntsLock) { _dadInts = value; } }
        }

        private Dictionary<string, List<string>> _transactionsManagersLeases = new Dictionary<string, List<string>>();
        private object _transactionsManagersLeasesLock = new object();

        public Dictionary<string, List <string>> TransactionsManagersLeases
        {
            get { lock (_transactionsManagersLeasesLock) { return _transactionsManagersLeases; } }
            set { lock (_transactionsManagersLeasesLock) { _transactionsManagersLeases = value; } }
        }

        //LM ID, Client
        private Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient> _lmsClients
            = new Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient>();

        public Dictionary<string, LMTMCommunicationService.LMTMCommunicationServiceClient> LmsClients
        {
            get { return _lmsClients; }
            set { _lmsClients = value; }
        }

        // TM ID, Client
        private Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> _tmsClients
            = new Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>>();

        public Dictionary<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> TmsClients
        {
            get { return _tmsClients; }
            set { _tmsClients = value; }
        }

        public TransactionManager()
        {
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
                return _leasesAvailable.Contains(lease);
            }
        }

        public void AddLeaseToList(string lease)
        {
            lock (_leaseListLock)
            {
                _leasesAvailable.Add(lease);
            }
        }

        public void RemoveLeaseToList(string lease)
        {
            lock (_leaseListLock)
            {
                _leasesAvailable.Remove(lease);
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

        public void AddLeaseSheet(Lease lease)
        {
            lock (_leaseSheetsLock)
            {
                _leaseSheet.Add(lease);
            }
        }

        public void PropagateLeaseResource(string tmIDtarget, List<string> leaseResource)
        {
            PropagateLeasesRequest request = new PropagateLeasesRequest();
            request.Lease.LeasedResources.AddRange(leaseResource);
            request.Lease.TmId = tmIDtarget;
            request.Id = PropagateId++;

            // checks if any transaction manager can respond to it in this timeslot
            lock (this)
            {
                foreach (var tm in TmsClients)
                {
                    if (!tm.Value.Item2.Contains(TimeSlot))
                    {
                        //if you dont suspect the tm at this timeslot you can ask for the leases
                        tm.Value.Item1.PropagateLeasesAsync(request);
                    }
                }
            }
        }

    }
}
