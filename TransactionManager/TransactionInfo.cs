namespace DADTKV.transactionManager
{
    class TransactionInfo
    {

        List<string> missingLeases = new List<string>();

        int _transactionID = 0;

        ClientTransactionRequest _transactionRequest = new ClientTransactionRequest();
        public ClientTransactionRequest TransactionRequest
        {
            get { lock (this) { return _transactionRequest; } }
            set { lock (this) { _transactionRequest = value; } }
        }

        ClientTransactionReply _transactionReply = new ClientTransactionReply();
        public ClientTransactionReply TransactionReply
        {
            get { lock (this) { return _transactionReply; } }
            set { lock (this) { _transactionReply = value; } }
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

        ManualResetEventSlim _signalClient = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalClient
        {
            get { lock (this) { return _signalClient; } }
            set { lock (this) { _signalClient = value; } }
        }

        ManualResetEventSlim signalLTM = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLTM
        {
            get { lock (this) { return signalLTM; } }
            set { lock (this) { signalLTM = value; } }
        }
    }
}
