namespace DADTKV.client
{
    class Client
    {
        private string _id = "";

        public string Id { get { return _id; } set { _id = value; } }

        private string _script = "";

        public string Script { get { return _script; } set { _script = value; } }

        private List<Tuple<string,string>> _tms = new List<Tuple<string,string>>();

        public List<Tuple<string,string>> Tms { get { return _tms; } set { _tms = value; } }

        public Client() { }
    }
}
