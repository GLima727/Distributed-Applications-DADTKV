using System.Timers;

namespace DADTKV.leaseManager
{
    class Paxos
    {
        private PaxosTuple _lmPaxosTuple = new PaxosTuple(null, 0, 0);
        public PaxosTuple LmPaxosTuple
        {
            get { return _lmPaxosTuple; }
            set { _lmPaxosTuple = value; }
        }

        private LeaseManager _lm;

        private int _paxosRoundN = 0;
        public int PaxosRoundN
        {
            get { return _paxosRoundN; }
            set { _paxosRoundN = value; }
        }

        public Paxos(LeaseManager lm)
        {
            _lm = lm;
        }

        static async Task<Promise> PrepareRequest(
                PaxosCommunicationService.PaxosCommunicationServiceClient client,
                PrepareRequest request)
        {
            return await client.SendPrepareAsync(request);
        }

        static async Task<Accepted> AcceptRequest(
                PaxosCommunicationService.PaxosCommunicationServiceClient client,
                AcceptRequest request)
        {
            return await client.SendAcceptAsync(request);
        }

        static async Task<ReceiveLeaseListResponse> SendLeaseSheet(
                LMTMCommunicationService.LMTMCommunicationServiceClient client,
                ReceiveLeaseListRequest request)
        {
            return await client.ReceiveLeaseListAsync(request);
        }

        public bool IsLeader()
        {
            if (_lm.LeaderId == _lm.Lms.Count())
                return true;

            foreach (var tuple in _lm.SusList)
            {
                int key = tuple.Item1;
                string value = tuple.Item2;

                // Check if the key already exists in the dictionary
                if (key == _paxosRoundN && value == _lm.PrevLeader)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsDown()
        {
            return _lm.RoundsDowns.Contains(_paxosRoundN);
        }

        public List<string> getSusList(int paxosRound)
        {
            var susList = new List<string>();
            foreach (var tuple in _lm.SusList)
            {
                int key = tuple.Item1;
                string value = tuple.Item2;

                // Check if the key already exists in the dictionary
                if (key == _paxosRoundN)
                {
                    susList.Add(value);
                }
            }

            return susList;
        }

        public async void PaxosRound(Object source, ElapsedEventArgs e)
        {
            _paxosRoundN++;

            if (IsLeader() && _lm.Buffer.Leases.Count() != 0 && !IsDown())
            {
                List<string> susList = getSusList(_paxosRoundN);

                _lmPaxosTuple = new PaxosTuple(_lm.Buffer, _lm.LeaderId / 10.0f, 0);
                _lm.Buffer = new LeaseList();

                int acceptsReceived = 0;

                while (acceptsReceived < _lm.LmsClients.Count / 2)
                {
                    // Prepare
                    int promisesReceived = 0;
                    DebugClass.Log("Prepare!");
                    while (promisesReceived < _lm.LmsClients.Count / 2)
                    {
                        promisesReceived = 0;
                        var reqsData = new PrepareRequest();

                        _lmPaxosTuple.WriteTimestamp = _lmPaxosTuple.WriteTimestamp + 1.0f;
                        reqsData.ReadTimestamp = _lmPaxosTuple.WriteTimestamp;

                        var tasksPrepare = new List<Task<Promise>>();

                        DebugClass.Log($"Send Prepare({reqsData.ReadTimestamp})");
                        // Send prepares
                        foreach (KeyValuePair<string, PaxosCommunicationService.PaxosCommunicationServiceClient> val in _lm.LmsClients)
                        {
                            if (!susList.Contains(val.Key))
                                tasksPrepare.Add(PrepareRequest(val.Value, reqsData));
                        }

                        // Waint for promises 
                        float highestRead = 0;
                        DebugClass.Log($"Prepare responses:");
                        foreach (Promise resq in await Task.WhenAll(tasksPrepare))
                        {
                            DebugClass.Log($"----({new PaxosTuple(resq.Val, 0, resq.ReadTimestamp)})");
                            if (resq.ReadTimestamp == -1)
                            {
                                continue;
                            }
                            else if (resq.Val != null && resq.ReadTimestamp > highestRead)
                            {
                                highestRead = resq.ReadTimestamp;
                                _lmPaxosTuple.Value = resq.Val;
                            }

                            promisesReceived++;
                        }
                    }

                    DebugClass.Log("Accept!");
                    // Send Accept
                    DebugClass.Log($"Send Accept({_lmPaxosTuple})");
                    var acceptData = new AcceptRequest();
                    acceptData.Val = _lmPaxosTuple.Value;
                    acceptData.WriteTimestamp = _lmPaxosTuple.WriteTimestamp;

                    var tasksAccept = new List<Task<Accepted>>();
                    foreach (KeyValuePair<string, PaxosCommunicationService.PaxosCommunicationServiceClient> val in _lm.LmsClients)
                    {
                        if (!susList.Contains(val.Key))
                            tasksAccept.Add(AcceptRequest(val.Value, acceptData));
                    }

                    // Wait for task to complete
                    acceptsReceived = 0;
                    foreach (Accepted resp in await Task.WhenAll(tasksAccept))
                    {
                        if (resp.Accepted_)
                        {
                            acceptsReceived++;
                        }
                    }
                }

                DebugClass.Log("commit");
            }
            else
            {
                _lmPaxosTuple = new PaxosTuple(null, 0, 0);
            }

        }
    }
}
