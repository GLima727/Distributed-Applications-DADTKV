namespace DADTKV.leaseManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            var lm = new LeaseManager();
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                    break;
                if (args[i].StartsWith("--") && !args[i + 1].StartsWith("--"))
                {
                    if (FlagReader.ArgumentActions.ContainsKey(args[i]))
                    {
                        FlagReader.ArgumentActions[args[i]](args[i + 1], lm);
                        i++;
                    }
                }
            }

            lm.Start();
        }
    }
}

