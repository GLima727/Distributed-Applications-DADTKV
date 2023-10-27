namespace DADTKV.transactionManager
{
    class TransactionEpoch
    {

        private TransactionManager _transactionManager;

        private int _epochIndex = 0;

        private object _epochIndexLock = new object();

        public int EpochIndex
        {
            get { lock (_epochIndexLock) { return _epochIndex; } }
            set { lock (_epochIndexLock) { _epochIndex = value; } }
        }

        private ManualResetEventSlim _epochSignal = new ManualResetEventSlim(false);
        private object _epochSignalLock = new object();

        public ManualResetEventSlim EpochSignal
        {
            get { lock (_epochSignalLock) { return _epochSignal; } }
            set { lock (_epochSignalLock) { _epochSignal = value; } }
        }

        private List<TransactionInfo> _transactionQueue = new List<TransactionInfo>();
        private object _transactionQueueLock = new object();

        public List<TransactionInfo> TransactionQueue
        {
            get { lock (_transactionQueueLock) { return _transactionQueue; } }
            set { lock (_transactionQueueLock) { _transactionQueue = value; } }
        }

        public TransactionEpoch(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
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
                    TransactionInfo transaction = new TransactionInfo();
                    foreach (var t in TransactionQueue)
                    {
                        if (t.TransactionID == lease.TransactionId)
                        {
                            transaction = t;
                            break;
                        }
                    }

                    _transactionManager.CurrentTrans = transaction;

                    // if is the first dont look back
                    if (lease_index == 0 && _transactionManager.TimeSlotN == 1)
                    {
                        _transactionManager.LeasesAvailable = transaction.MissingLeases;
                        DebugClass.Log("[Run] I was the first to receive this lease.");
                    }
                    else
                    {
                        // check if someone have the leases we need
                        var missingLeases = lookBackLeases(lease, lease_index, leaseSheet);

                        var leases_to_add = transaction.MissingLeases.Except(missingLeases).ToList();
                        transaction.MissingLeases = missingLeases;

                        foreach (var l in leases_to_add)
                        {
                            if (!_transactionManager.LeasesAvailable.Contains(l))
                                _transactionManager.LeasesAvailable.Add(l);
                        }

                        foreach (var l in transaction.MissingLeases)
                        {
                            DebugClass.Log($"[Run] missing {l}");
                        }

                        if (transaction.MissingLeases.Count != 0)
                        {
                            DebugClass.Log("[Run] we need to wait to get leases from others tms");
                            // Wait for others tm to give leases
                            transaction.SignalLTM.Wait();
                            transaction.SignalLTM.Reset();
                        }
                    }

                    transaction.TransactionReply = _transactionManager.executeOperations(transaction.TransactionRequest);
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

                        // remove A from ("A","B") and so on
                        foreach (string resource in leases.Value)
                        {
                            DebugClass.Log($"[Run] Remove lease {resource}.");
                            _transactionManager.LeasesAvailable.Remove(resource);
                        }
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
