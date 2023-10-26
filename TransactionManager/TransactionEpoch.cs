using Grpc.Core;
using Grpc.Net.Client;
using System.Security.Cryptography;

namespace DADTKV.transactionManager
{
    class TransactionEpoch
    {

        private TransactionManager _transactionManager;

        public TransactionEpoch(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        //public void Run()
        //{

        //    _transactionManager.NumberLms = 0;
        //    DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Sent lease requests.");
        //    // Wait to receive lease sheet
        //    //info.SignalLSheet.Wait();
        //    DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] Received lease sheet.");
        //    //info.SignalLSheet.Reset();

        //    List<Lease> leaseSheet = _transactionManager.LeaseSheet;
        //    // send lms for the lease sheet but check if its down

        //    int lease_index = 0;

        //    // For each Lease i received
        //    foreach (Lease lease in leaseSheet)
        //    {
        //        // Check if this lease is for this tmId
        //        if (lease.TmId == _transactionManager.Id && lease.TransactionId == _transactionManager.TransactionID)
        //        {
        //            // if is the first dont look back
        //            if (lease_index == 0 && _transactionManager.NRound == 1)
        //            {
        //                //_transactionManager.LeasesAvailable = info.MissingLeases;
        //                DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] I was the first to receive this lease.");
        //            }
        //            else
        //            {
        //                // check if someone have the leases we need
        //                var missingLeases = lookBackLeases(lease, lease_index, leaseSheet);

        //                var leases_to_add = info.MissingLeases.Except(missingLeases).ToList();
        //                info.MissingLeases = missingLeases;

        //                foreach (var l in leases_to_add)
        //                {
        //                    if (!_transactionManager.LeasesAvailable.Contains(l))
        //                        _transactionManager.LeasesAvailable.Add(l);
        //                }

        //                foreach (var l in info.MissingLeases)
        //                {
        //                    DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] missing {l}");
        //                }

        //                if (info.MissingLeases.Count != 0)
        //                {
        //                    DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] we need to wait to get leases from others tms");
        //                    // Wait for others tm to give leases
        //                    info.SignalLTM.Wait();
        //                    info.SignalLTM.Reset();
        //                }
        //            }

        //            reply = executeOperations(request);

        //            // Send Leases to anyone who needs it
        //            DebugClass.Log("[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases]");
        //            Dictionary<string, List<string>> leasesToSend = lookAheadLeases(lease, lease_index, leaseSheet);
        //            foreach (KeyValuePair<string, List<string>> leases in leasesToSend)
        //            {
        //                DebugClass.Log($"[SubmitTransactionImpl] [Make transaction] [Solve missing leases] [Send leases] sending {leases.Key}");

        //                // If we need to send leases to our selfs skip
        //                if (leases.Key == _transactionManager.Id)
        //                {
        //                    continue;
        //                }

        //                // im checking the suspicion list inside this
        //                // here you ask for leases but dont send the request if you suspect the one you are asking
        //                _transactionManager.PropagateLeaseResource(leases.Key, leases.Value);

        //                // remove A from ("A","B") and so on
        //                foreach (string resource in leases.Value)
        //                {
        //                    _transactionManager.RemoveLeaseFromAvailableList(resource);
        //                }
        //            }

        //            // we don't need to see more leases
        //            break;
        //        }
        //        lease_index++;
        //    }



        //    while (true) ;
        //}

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