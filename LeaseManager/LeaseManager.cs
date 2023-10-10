using Grpc.Core;
using Grpc.Net.Client;

namespace DADTKV.leaseManager
{
    class LeaseManager
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

        // Private only atributtes
        private int _leaderId = -1;

        private Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient> _lmsClients =
            new Dictionary<string, PaxosCommunicationService.PaxosCommunicationServiceClient>();

        private List<LMTMCommunicationService.LMTMCommunicationServiceClient> _tmsClients =
            new List<LMTMCommunicationService.LMTMCommunicationServiceClient>();

        public LeaseManager() { }

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
                    _leaderId = _lms.Count - i;
                }
            }
        }

        public void createConnectionsToTms()
        {
            // Create connections to other Transmissions Managers
            foreach (var tm in _tms)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(tm.Item2);
                var client = new LMTMCommunicationService.LMTMCommunicationServiceClient(channel);
                _tmsClients.Add(client);
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
            ServerPort serverPort = new ServerPort(_url, _port, ServerCredentials.Insecure);
            Server server = new Server
            {
                Services = {
                    PaxosCommunicationService.BindService(new PaxosService(this)),
                    LMTMCommunicationService.BindService(new (this))
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
