namespace DADTKV.leaseManager
{
    /// <summary>
    /// Represents the tuple used in the Paxos algorithm.
    /// </summary>
    class PaxosTuple
    {

        /// <summary>
        /// Value of the tuple. 
        /// </summary>
        private LeaseList? _val;
        public LeaseList? Value
        {
            get
            {
                lock (this)
                {
                    return _val;
                }
            }
            set
            {
                lock (this)
                {
                    _val = value;
                }
            }
        }

        /// <summary>
        /// Is used to determine if a proposed val 
        /// is more recent than any previously accepted val. 
        /// </summary>
        private float _writeTimestamp;
        public float WriteTimestamp
        {
            get
            {
                lock (this)
                {
                    return _writeTimestamp;
                }
            }
            set
            {
                lock (this)
                {
                    _writeTimestamp = value;
                }
            }
        }

        /// <summary>
        /// Is used to ensure that a read operation returns the 
        /// most recent val.
        /// </summary>
        private float _readTimestamp;
        public float ReadTimestamp
        {
            get
            {
                lock (this)
                {
                    return _readTimestamp;
                }
            }
            set
            {
                lock (this)
                {
                    _readTimestamp = value;
                }
            }
        }

        public PaxosTuple(LeaseList? inVal, float inWriteTimestamp, float inReadTimestamp)
        {
            _val = inVal;
            _writeTimestamp = inWriteTimestamp;
            _readTimestamp = inReadTimestamp;
        }

        /// <summary>
        /// Print the tuple.
        /// </summary>
        public override string ToString()
        {
            string leases = "";
            if (_val != null)
            {
                foreach (var lease in _val.Leases.ToList())
                {
                    foreach (var l in lease.LeasedResources.ToList())
                    {
                        leases += $"{lease.TmId}, {l.ToString()}";
                    }
                }
                leases += "|";
            }
            else
            {
                leases = "null";
            }


            return $"<{leases}, {_writeTimestamp}, {_readTimestamp}>";
        }

    }
}
