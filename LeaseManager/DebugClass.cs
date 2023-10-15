namespace DADTKV.leaseManager
{
    static class DebugClass
    {

        public static bool IsEnabled { get; set; } = false;

        public static void Log(string message)
        {
            if (!IsEnabled)
            {
                return;
            }

            Console.Write("\u001b[1m\u001b[32m["); // Start with green '['
            Console.Write("\u001b[0m");   // Reset color to default
            Console.Write("Lease Manager");
            Console.Write("\u001b[32m]\u001b[0m"); // End with green ']'
            Console.Write("\u001b[0m ");   // Reset color to default
            Console.Write(message + "\n");
        }
    }
}

