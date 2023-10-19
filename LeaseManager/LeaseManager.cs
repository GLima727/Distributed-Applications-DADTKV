using Grpc.Core;
using Grpc.Net.Client;

namespace DADTKV.leaseManager
{
    /// <summary>
    /// This class represents a Lease Manager, responsible for managing leases in the system.
    /// </summary>
    class LeaseManager
    {
        /// <summary>
        /// Gets or sets the unique identifier for this Lease Manager instance.
        /// </summary>
        private string _id = "";
        public string Id { get { return _id; } set { _id = value; } }

        /// <summary>
        /// Gets or sets the URL of the Lease Manager.
        /// </summary>
        private string _url = "";
        public string Url { get { return _url; } set { _url = value; } }

        /// <summary>
        /// Gets or sets the port number for communication.
        /// </summary>
        private int _port = 0;
        public int Port { get { return _port; } set { _port = value; } }

        /// <summary>
        /// Gets or sets the time slot for lease management.
        /// </summary>
        private int _timeSlot = 0;
        public int TimeSlot { get { return _timeSlot; } set { _timeSlot = value; } }

        /// <summary>
        /// Gets or sets the number of slots.
        /// </summary>
        private int _numSlot = 0;
        public int NumSlot { get { return _numSlot; } set { _numSlot = value; } }

        /// <summary>
        /// Gets or sets the list of round downs.
        /// </summary>
        private List<int> _roundsDowns = new List<int>();
        public List<int> RoundsDowns { get { return _roundsDowns; } set { _roundsDowns = value; } }

        /// <summary>
        /// Gets or sets the start time for lease management.
        /// </summary>
        private string _timeStart = "";
        public string TimeStart { get { return _timeStart; } set { _timeStart = value; } }

        /// <summary>
        /// Gets or sets the list of Transmission Managers (TMs).
        /// </summary>
        private List<Tuple<string, string>> _tms = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> Tms { get { return _tms; } set { _tms = value; } }

        /// <summary>
        /// Gets or sets the list of Lease Managers (LMs).
        /// </summary>
        private List<Tuple<string, string>> _lms = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> Lms { get { return _lms; } set { _lms = value; } }

        /// <summary>
        /// Gets or sets the list of suspended leases.
        /// </summary>
        private List<Tuple<int, string>> _susList = new List<Tuple<int, string>>();
        public List<Tuple<int, string>> SusList { get { return _susList; } set { _susList = value; } }

        /// <summary>
        /// Gets the leader ID.
        /// </summary>
        /// <remarks>
        /// This is a private attribute used internally.
        /// </remarks>
        private int _leaderId = -1;
        public int LeaderId
        {
            get { return _leaderId; }
            set { _leaderId = value; }
        }

        /// <summary>
        /// Dictionary containing connections to other Lease Managers (LMs).
        /// </summary>
        /// <remarks>
        /// Key: LM ID, Value: Grpc Client.
        /// </remarks>
        private Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient> _lmsClients =
            new Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient>();
        public Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient> LmsClients
        {
            get { return _lmsClients; }
            set { _lmsClients = value; }
        }

        /// <summary>
        /// List of connections to Transmission Managers (TMs).
        /// </summary>
        private List<LMTMCommunicationService.LMTMCommunicationServiceClient> _tmsClients =
            new List<LMTMCommunicationService.LMTMCommunicationServiceClient>();
        public List<LMTMCommunicationService.LMTMCommunicationServiceClient> TmsClients
        {
            get { return _tmsClients; }
            set { _tmsClients = value; }
        }

        /// <summary>
        /// Current round number.
        /// </summary>
        /// <remarks>
        /// This is a private attribute used internally.
        /// </remarks>
        private int _current_round = 0;

        private LeaseList _buffer = new LeaseList();
        public LeaseList Buffer
        {
            get { lock (_bufferLock) { return _buffer; } }
            set { lock (_bufferLock) { _buffer = value; } }
        }

        private object _bufferLock = new object();
        public object BufferLock
        {
            get { return _bufferLock; }
            set { _bufferLock = value; }
        }

        private Paxos _lmPaxos;
        public Paxos LmPaxos
        {
            get { lock (_paxosLock) { return _lmPaxos; } }
            set { lock (_paxosLock) { _lmPaxos = value; } }
        }

        private object _paxosLock = new object();
        public object PaxosLock
        {
            get { return _paxosLock; }
            set { _paxosLock = value; }
        }

        private string _prevLeader = "";
        public string PrevLeader { get { return _prevLeader; } set { _prevLeader = value; } }

        private System.Timers.Timer _paxosClock = new System.Timers.Timer();

        /// <summary>
        /// Default constructor for Lease Manager.
        /// </summary>
        public LeaseManager()
        {
            _lmPaxos = new Paxos(this);
        }

        public void AddLeaseToBuffer(Lease l)
        {
            lock (_bufferLock)
            {
                _buffer.Leases.Add(l);
            }
        }

        /// <summary>
        /// Creates connections to other Lease Managers (LMs).
        /// </summary>
        private void createConnectionsToLms()
        {
            // Create connections to other Lease Managers
            for (int i = 0; i < _lms.Count; i++)
            {
                if (_lms[i].Item1 != _id)
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(_lms[i].Item2);
                    var client = new PaxosCommunicationService.PaxosCommunicationServiceClient(channel);
                    _lmsClients[_lms[i].Item1] = client;
                }
                else
                {
                    if (i != 0)
                    {
                        _prevLeader = _lms[i - 1].Item1;
                    }
                    _leaderId = _lms.Count - i;
                }
            }
        }

        /// <summary>
        /// Checks if the Lease Manager has crashed.
        /// </summary>
        /// <returns>True if crashed, otherwise false.</returns>
        private bool hasCrashed()
        {
            return _roundsDowns.Contains(_current_round);
        }

        /// <summary>
        /// Creates connections to other Transmission Managers (TMs).
        /// </summary>
        private void createConnectionsToTms()
        {
            // Create connections to other Transmissions Managers
            foreach (var tm in _tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm.Item2);
                var client = new LMTMCommunicationService.LMTMCommunicationServiceClient(channel);
                _tmsClients.Add(client);
            }
        }

        /// <summary>
        /// Waits for the specified start time.
        /// </summary>
        private void WaitForStartTime()
        {
            DateTime startTime = DateTime.ParseExact(TimeStart, "HH:mm:ss", null);

            while (DateTime.Now < startTime)
            {
                // Wait for a short period of time before checking again
                System.Threading.Thread.Sleep(100); // Sleep for 1 second (adjust as needed)
            }
        }

        /// <summary>
        /// Starts the Lease Manager.
        /// </summary>
        public void Start()
        {
            DebugClass.Log($"Start Lease Manager {_id}.");
            // Create Server 
            ServerPort serverPort = new ServerPort(_url, _port, ServerCredentials.Insecure);
            Server server = new Server
            {
                Services = {
                    PaxosCommunicationService.BindService(new PaxosService(this)),
                    LMTMCommunicationService.BindService(new LMTMService(this))
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

            // Start the clock
            _paxosClock.Interval = _timeSlot;
            _paxosClock.AutoReset = true;
            _paxosClock.Elapsed += _lmPaxos.PaxosRound;
            _paxosClock.Start();

            while (true) ;
        }
    }
}

