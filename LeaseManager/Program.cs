namespace DADTKV.leaseManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            var lm = new LeaseManager();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    if (FlagReader.ArgumentActions.ContainsKey(args[i]))
                    {
                        FlagReader.ArgumentActions[args[i]](args[i + 1], lm);
                        i++;
                    }
                }
            }

            Console.WriteLine(lm.Id);
        }
    }
}

