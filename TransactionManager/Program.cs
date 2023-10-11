namespace DADTKV.transactionManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            var tm = new TransactionManager();
            for (int i = 0; i < args.Length; i++)
            {
                if (i + 1 >= args.Length)
                    break;
                if (args[i].StartsWith("--") && !args[i + 1].StartsWith("--"))
                {
                    if (FlagReader.ArgumentActions.ContainsKey(args[i]))
                    {
                        FlagReader.ArgumentActions[args[i]](args[i + 1], tm);
                        i++;
                    }
                }
            }
            
            DebugClass.IsEnabled = true;
            tm.Start();
        }
    }
}

