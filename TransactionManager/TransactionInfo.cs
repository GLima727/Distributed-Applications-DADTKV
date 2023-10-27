namespace DADTKV.transactionManager
{
    class TransactionInfo
    {

        List<string> missingLeases = new List<string>();

        int _transactionID = 0;

        ClientTransactionRequest _transactionRequest = new ClientTransactionRequest();
        public ClientTransactionRequest TransactionRequest
        {
            get { return _transactionRequest; }
            set { _transactionRequest = value; }
        }

        ClientTransactionReply _transactionReply = new ClientTransactionReply();
        public ClientTransactionReply TransactionReply
        {
            get { return _transactionReply; }
            set { _transactionReply = value; }
        }

        public int TransactionID
        {
            get { return _transactionID; }
            set { _transactionID = value; }
        }
        public List<string> MissingLeases
        {
            get { return missingLeases; }
            set { missingLeases = value; }
        }

        ManualResetEventSlim _signalClient = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalClient
        {
            get { return _signalClient; }
            set { _signalClient = value; }
        }

        ManualResetEventSlim signalLTM = new ManualResetEventSlim(false);
        public ManualResetEventSlim SignalLTM
        {
            get { return signalLTM; }
            set { signalLTM = value; }
        }

        public int status = 0;

    }
}
