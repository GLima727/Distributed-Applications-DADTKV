namespace DADTKV.client
{
    class Program
    {
        public static void Main(string[] args)
        {
            Client cl = new Client();
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                    break;
                if (args[i].StartsWith("--") && !args[i + 1].StartsWith("--"))
                {
                    if (FlagReader.ArgumentActions.ContainsKey(args[i]))
                    {
                        FlagReader.ArgumentActions[args[i]](args[i + 1], cl);
                        i++;
                    }
                }
            }
        }
    }
}
