namespace DADTKV.transactionManager
{
    class TransactionInfo
    {
        ClientTransactionRequest _clientTransactionRequest = new ClientTransactionRequest();

        List<string> missingLeases = new List<string>();

        int _transactionID = 0;

        public ClientTransactionRequest ClientTransactionRequest
        {
            get { lock (this) { return _clientTransactionRequest; } }
            set { lock (this) { _clientTransactionRequest = value; } }
        }
        public int TransactionID
        {
            get { lock (this) { return _transactionID; } }
            set { lock (this) { _transactionID = value; } }
        }
        public List<string> MissingLeases
        {
            get { lock (this) { return missingLeases; } }
            set { lock (this) { missingLeases = value; } }
        }

        ManualResetEventSlim signalLSheet = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLSheet
        {
            get { lock (this) { return signalLSheet; } }
            set { lock (this) { signalLSheet = value; } }
        }

        ManualResetEventSlim signalLTM = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLTM
        {
            get { lock (this) { return signalLTM; } }
            set { lock (this) { signalLTM = value; } }
        }
    }
}
