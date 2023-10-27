namespace DADTKV.transactionManager
{
    class TransactionEpoch
    {

        private TransactionManager _transactionManager;

        private int _epochIndex = 0;

        private object _epochIndexLock = new object();

        public int EpochIndex
        {
            get { return _epochIndex; }
            set { _epochIndex = value; }
        }

        private ManualResetEventSlim _epochSignal = new ManualResetEventSlim(false);
        private object _epochSignalLock = new object();

        public ManualResetEventSlim EpochSignal
        {
            get { return _epochSignal; }
            set { _epochSignal = value; }
        }

        private List<TransactionInfo> _transactionQueue = new List<TransactionInfo>();
        private object _transactionQueueLock = new object();

        public List<TransactionInfo> TransactionQueue
        {
            get { return _transactionQueue; }
            set { _transactionQueue = value; }
        }

        public TransactionEpoch(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        private TransactionInfo lookForTransaction(int id)
        {
            foreach (var t in TransactionQueue)
            {
                if (t.TransactionID == id)
                {
                    return t;
                }
            }

            return new TransactionInfo();
        }

        public void Run(List<Lease> leaseSheet)
        {
            DebugClass.Log("[Run] start.");

            DebugClass.Log($"[Run] have {TransactionQueue.Count}.");

            DebugClass.Log($"[Run] transaction.");
            // For each Lease i received
            int lease_index = 0;
            foreach (Lease lease in leaseSheet)
            {
                // Check if this lease is for this tmId
                if (lease.TmId == _transactionManager.Id)
                {
                    var transaction = lookForTransaction(lease.TransactionId);
                    _transactionManager.CurrentTrans = transaction;

                    // check if someone have the leases we need before us
                    var missingLeases = lookBackLeases(lease, lease_index, leaseSheet);

                    // add the ones we saw that nobody have 
                    var leases_to_add = transaction.MissingLeases.Except(missingLeases).ToList();
                    transaction.MissingLeases = missingLeases;

                    // add that ones 
                    Monitor.Enter(_transactionManager.TMLock);
                    foreach (var l in leases_to_add)
                    {
                        if (!_transactionManager.LeasesAvailable.Contains(l))
                            _transactionManager.LeasesAvailable.Add(l);
                    }
                    Monitor.Exit(_transactionManager.TMLock);

                    // before wait check again what i am missing
                    foreach (var l in _transactionManager.LeasesAvailable)
                    {
                        DebugClass.Log($"[Run] we have {l}");
                        if (transaction.MissingLeases.Contains(l))
                        {
                            transaction.MissingLeases.Remove(l);
                        }
                    }

                    while (transaction.MissingLeases.Count != 0)
                    {
                        DebugClass.Log("[Run] we need to wait to get leases from others tms");
                        // Wait for others tm to give leases
                        bool eventSet = transaction.SignalLTM.Wait(_transactionManager.TimeSlotD * 2);

                        if (!eventSet)
                        {
                            foreach (var t in TransactionQueue)
                            {
                                t.status = -1;
                                t.SignalClient.Set();
                            }

                            return;
                        }

                        Monitor.Enter(_transactionManager.TMLock);
                        foreach (var l in _transactionManager.LeasesAvailable)
                        {
                            if (transaction.MissingLeases.Contains(l))
                            {
                                transaction.MissingLeases.Remove(l);
                            }
                        }
                        Monitor.Exit(_transactionManager.TMLock);

                        transaction.SignalLTM.Reset();
                    }

                    transaction.TransactionReply = _transactionManager.executeOperations(transaction.TransactionRequest);
                    transaction.status = 1;
                    TransactionQueue.Remove(transaction);
                    transaction.SignalClient.Set();

                    // Send Leases to anyone who needs it
                    Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index, leaseSheet);
                    foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                    {
                        DebugClass.Log($"[Run] sending {leases.Key}.");

                        // If we need to send leases to our selfs skip
                        if (leases.Key == _transactionManager.Id)
                        {
                            DebugClass.Log($"[Run] self. ");
                            continue;
                        }

                        // im checking the suspicion list inside this
                        // here you ask for leases but dont send the request if you suspect the one you are asking
                        _transactionManager.PropagateLeaseResource(leases.Key, leases.Value);

                        Monitor.Enter(_transactionManager.TMLock);
                        // remove A from ("A","B") and so on
                        foreach (string resource in leases.Value)
                        {
                            DebugClass.Log($"[Run] Remove lease {resource}.");
                            _transactionManager.LeasesAvailable.Remove(resource);
                        }
                        Monitor.Exit(_transactionManager.TMLock);
                    }
                }

                lease_index++;
            }
            // _transactionManager.TransactionEpochList[EpochIndex - 1].EpochSignal.Set();
            DebugClass.Log($"[Run] Exit.");
        }

        public Dictionary<string, List<string>> lookAheadLeases(Lease lease, int lease_index, List<Lease> leaseSheet)
        {
            Dictionary<string, List<string>> leasesToSend = new Dictionary<string, List<string>>();
            foreach (string resource in lease.LeasedResources)
            {
                for (int i = lease_index + 1; i < leaseSheet.Count; i++)
                {
                    if (leaseSheet[i].LeasedResources.Contains(resource))
                    {
                        // atencao verificar se ele nao esta a criar ids repetidos no dicionario
                        if (!leasesToSend.ContainsKey(leaseSheet[i].TmId))
                        {
                            leasesToSend[leaseSheet[i].TmId] = new List<string>();
                        }
                        leasesToSend[leaseSheet[i].TmId].Add(resource);

                        break;
                    }
                }

            }

            return leasesToSend;
        }

        public List<string> lookBackLeases(Lease lease, int lease_index, List<Lease> leaseSheet)
        {
            List<string> missingLeases = new List<string>();

            // for each resource we want
            // A B
            DebugClass.Log("[LookBack]");
            foreach (string resource in lease.LeasedResources)
            {
                DebugClass.Log($"[LookBack] {resource}");
                // we look for the back to see if someone have the lease we need  
                for (int i = lease_index - 1; i >= 0; i--)
                {
                    DebugClass.Log($"[LookBack] {lease_index}");
                    if (leaseSheet[i].LeasedResources.Contains(resource))
                    {
                        if (leaseSheet[i].TmId != _transactionManager.Id)
                        {
                            DebugClass.Log($"[LookBack] added {resource}");
                            missingLeases.Add(resource);
                        }
                        else
                        {
                            DebugClass.Log($"[LookBack] we have the lease {resource}");
                        }
                        break;
                    }
                }
            }
            return missingLeases;
        }
    }
}
