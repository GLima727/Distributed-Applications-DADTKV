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

        public static void TmsReader(string arg, Client cl)
        {
            List<string> Tms = new List<string>();
            foreach (string transMan in arg.Split(','))
            {
                if (transMan != "")
                {
                    string[] elems = transMan.Split("%");
                    Tms.Add(elems[1]);
                }

            }
            cl.Tms = Tms;
        }
    }
}
