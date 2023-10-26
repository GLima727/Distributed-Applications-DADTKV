using System.Timers;

using Grpc.Core;
using Grpc.Net.Client;

namespace DADTKV.transactionManager
{
    class TransactionManager
    {
        private System.Timers.Timer _clock = new System.Timers.Timer();

        private string _id = "";
        public string Id { get { return _id; } set { _id = value; } }

        private string _url = "";
        public string Url { get { return _url; } set { _url = value; } }

        private int _port = 0;

        public int Port { get { return _port; } set { _port = value; } }

        private int _timeSlot = 0;
        private object _timeSlotLock = new object();
        public int TimeSlot { get { lock (_timeSlotLock) { return _timeSlot; } } set { lock (_timeSlotLock) { _timeSlot = value; } } }

        private int _nRound = 0;
        private object _nRoundlock = new object();
        public int NRound { get { lock (_nRoundlock) { return _nRound; } } set { lock (_nRoundlock) { _nRound = value; } } }


        private int _propagateId = 0;
        public int PropagateId { get { return _propagateId; } set { _propagateId = value; } }

        private int _numSlot = 0;
        public int NumSlot { get { return _numSlot; } set { _numSlot = value; } }

        private int _currentRound = 0;
        private object _currentRoundLock = new object();
        public int CurrentRound { get { lock (_currentRoundLock) { return _currentRound; } } set { lock (_currentRoundLock) { _currentRound = value; } } }

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

        private List<string> _leasesAvailable = new List<string>();
        private object _leaseListLock = new object();
        public List<string> LeasesAvailable
        {
            get { lock (_leaseListLock) { return _leasesAvailable; } }
            set { lock (_leaseListLock) { _leasesAvailable = value; } }
        }

        private int _numberLms;
        public int NumberLms { get { return _numberLms; } set { _numberLms = value; } }

        private TransactionInfo _currentTrans = new TransactionInfo();
        private object _currentTransLock = new object();
        public TransactionInfo CurrentTrans
        {
            get { lock (_currentTransLock) { return _currentTrans; } }
            set { lock (_currentTransLock) { _currentTrans = value; } }
        }

        private List<TransactionEpoch> _transactionEpochList = new List<TransactionEpoch>();
        private object _transactionEpochLock = new object();
        public List<TransactionEpoch> TransactionEpochList
        {
            get { lock (_transactionEpochLock) { return _transactionEpochList; } }
            set { lock (_transactionEpochLock) { _transactionEpochList = value; } }
        }

        private Dictionary<string, int> _dadInts = new Dictionary<string, int>();
        private object _dadIntsLock = new object();
        public Dictionary<string, int> DadInts
        {
            get { lock (_dadIntsLock) { return _dadInts; } }
            set { lock (_dadIntsLock) { _dadInts = value; } }
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

        private List<string> _acksReceived = new List<string>();
        private object _acksReceivedLock = new object();
        public List<string> AcksReceived
        {
            get { lock (_acksReceivedLock) { return _acksReceived; } }
            set { lock (_acksReceivedLock) { _acksReceived = value; } }
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

            // Create first epoch
            for (int i = 0; i < _numSlot; i++)
                _transactionEpochList.Add(new TransactionEpoch(this));

            server.Start();
            DebugClass.Log("Set connections to another LMs.");
            createConnectionsToLms();
            DebugClass.Log("Set connections to TMs.");
            createConnectionsToTms();

            DebugClass.Log("Waiting for wall time.");
            WaitForStartTime();
            DebugClass.Log("Wall time completed.");

            // Start the clock
            _clock.Interval = _timeSlot;
            _clock.AutoReset = true;
            _clock.Elapsed += TimeSlotRound;
            _clock.Start();


            while (true) ;
        }

        public void TimeSlotRound(Object source, ElapsedEventArgs e)
        {
            CurrentRound++;
            DebugClass.Log("New timeslot");
        }

        public void PropagateLeaseResource(string tmIDtarget, List<string> leaseResource)
        {
            DebugClass.Log("[Propagate Lease tm function]");

            PropagateLeasesRequest request = new PropagateLeasesRequest();
            request.Lease = new Lease();
            request.Lease.LeasedResources.AddRange(leaseResource);
            request.Lease.TmId = tmIDtarget;
            request.Id = ++PropagateId;
            request.SenderId = Id;

            // checks if any transaction manager can respond to it in this timeslot
            lock (this)
            {
                foreach (var tm in TmsClients)
                {
                    if (!tm.Value.Item2.Contains(TimeSlot) && tm.Key != Id)
                    {
                        DebugClass.Log($"[Propagate Lease tm function] send lease {tm.Key} {tm.Value.Item2}");
                        //if you dont suspect the tm at this timeslot you can ask for the leases
                        tm.Value.Item1.PropagateLeasesAsync(request);
                    }
                }
            }
        }

        public void URBroadCastMemory(List<DADInt> message)
        {
            DebugClass.Log("[URBroadCast]");

            foreach (KeyValuePair<string, Tuple<CrossServerTransactionManagerService.CrossServerTransactionManagerServiceClient, List<int>>> tm
                in TmsClients)
            {
                //if tmClient == alive?
                URBroadCastRequest urbroadcastRequest = new URBroadCastRequest();

                foreach (DADInt m in message)
                {
                    DebugClass.Log($"[URBroadCast] DAdiNT {m}");
                }

                urbroadcastRequest.Sender = Id;
                urbroadcastRequest.Message.AddRange(message);
                urbroadcastRequest.TimeStamp = TimeSlot;
                tm.Value.Item1.URBroadCast(urbroadcastRequest);
            }
        }

        public ClientTransactionReply executeOperations(ClientTransactionRequest request)
        {
            ClientTransactionReply reply = new ClientTransactionReply();

            DebugClass.Log("[ExecuteOP] Execute operations");
            // Execute reading operations.
            foreach (string readOp in request.ReadOperations)
            {
                if (!DadInts.ContainsKey(readOp))
                {
                    var resp = new DADInt();
                    reply.ObjValues.Add(resp);
                }
                else
                {
                    var resp = new DADInt();
                    resp.Key = readOp;
                    resp.Value = DadInts[readOp];
                    reply.ObjValues.Add(resp);
                }
            }

            // Execute writting operations.
            foreach (DADInt dadInt in request.WriteOperations)
            {
                DadInts[dadInt.Key] = dadInt.Value;
            }

            if (request.WriteOperations.ToList().Count() != 0)
            {
                // Propagate WriteOperations
                URBroadCastMemory(request.WriteOperations.ToList());
            }
            return reply;
        }
    }
}
