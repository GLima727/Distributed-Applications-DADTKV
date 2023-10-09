namespace DADTKV.client
{
    class FlagReader
    {
        public static Dictionary<string, Action<string, Client>> ArgumentActions = new Dictionary<string, Action<string, Client>>
        {
            {"--id", IdReader},
            {"--script", ScriptReader},
            {"--tms", TmsReader},
        };

        public static void IdReader(string arg, Client cl)
        {
            cl.Id = arg; 
        }

        public static void ScriptReader(string arg, Client cl)
        {
            cl.Script = arg;
        }

        public static void TmsReader(string arg, Client tm)
        {
            List<Tuple<string, string>> Tms = new List<Tuple<string, string>>();
            foreach (string transMan in arg.Split(','))
            {
                if (transMan != "")
                {
                    string[] elems = transMan.Split("%");
                    Tms.Add(new Tuple<string, string>(elems[0], elems[1]));
                }

            }
            tm.Tms = Tms;
        }
    }
}
