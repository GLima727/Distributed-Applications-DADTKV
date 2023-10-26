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

        private Queue<TransactionInfo> _transactionQueue = new Queue<TransactionInfo>();
        private object _transactionQueueLock = new object();

        public Queue<TransactionInfo> TransactionQueue
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
            while (TransactionQueue.Count > 0)
            {
                var transaction = TransactionQueue.Dequeue();
                _transactionManager.CurrentTrans = transaction;


                int lease_index = 0;
                // For each Lease i received
                foreach (Lease lease in leaseSheet)
                {
                    // Check if this lease is for this tmId
                    if (lease.TmId == _transactionManager.Id && lease.TransactionId == transaction.TransactionID)
                    {
                        // if is the first dont look back
                        if (lease_index == 0 && _transactionManager.NRound == 1)
                        {
                            _transactionManager.LeasesAvailable = transaction.MissingLeases;
                            DebugClass.Log("[Epoch] [Make transaction] [Solve missing leases] I was the first to receive this lease.");
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
                                DebugClass.Log($"[Epoch] [Make transaction] [Solve missing leases] missing {l}");
                            }

                            if (transaction.MissingLeases.Count != 0)
                            {
                                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] we need to wait to get leases from others tms");
                                // Wait for others tm to give leases
                                transaction.SignalLTM.Wait();
                                transaction.SignalLTM.Reset();
                            }
                        }

                        //info.ClientTransactionReply = _transactionManager.executeOperations(info.ClientTransactionRequest);
                        transaction.TransactionReply = _transactionManager.executeOperations(transaction.TransactionRequest);
                        transaction.SignalClient.Set();

                        // Send Leases to anyone who needs it
                        DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases]");
                        Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index, leaseSheet);
                        foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
                        {
                            DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases] sending {leases.Key}");

                            // If we need to send leases to our selfs skip
                            if (leases.Key == _transactionManager.Id)
                            {
                                continue;
                            }

                            // im checking the suspicion list inside this
                            // here you ask for leases but dont send the request if you suspect the one you are asking
                            _transactionManager.PropagateLeaseResource(leases.Key, leases.Value);

                            // remove A from ("A","B") and so on
                            foreach (string resource in leases.Value)
                            {
                                _transactionManager.LeasesAvailable.Remove(resource);
                            }
                        }

                    }

                    // we don't need to see more leases
                    break;
                }
                lease_index++;
            }
            // _transactionManager.TransactionEpochList[EpochIndex - 1].EpochSignal.Set();
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
