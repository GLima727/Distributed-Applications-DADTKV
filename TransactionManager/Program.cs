namespace DADTKV.transactionManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            var tm = new TransactionManager();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {

                    if (FlagReader.ArgumentActions.ContainsKey(args[i]))
                    {
                        FlagReader.ArgumentActions[args[i]](args[i + 1], tm);
                        i++;
                    }
                }
            }

            Console.WriteLine(tm.Id);
        }
    }
}

