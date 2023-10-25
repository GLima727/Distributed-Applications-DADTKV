namespace DADTKV.transactionManager
{
    static class DebugClass
    {

        public static bool IsEnabled { get; set; } = false;
        public static bool IsEnabled1 { get; set; } = false;

        public static bool IsEnabled2 { get; set; } = false;

        public static void Log(string message)
        {
            if (!IsEnabled)
            {
                return;
            }

            Console.Write("\u001b[1m\u001b[31m["); // Start with green '['
            Console.Write("\u001b[0m");   // Reset color to default
            Console.Write("Transaction Manager");
            Console.Write("\u001b[31m]\u001b[0m"); // End with green ']'
            Console.Write("\u001b[0m ");   // Reset color to default
            Console.Write(message + "\n");
        }

        public static void Log1(string message)
        {
            if (!IsEnabled1)
            {
                return;
            }

            Console.Write("\u001b[1m\u001b[31m["); // Start with green '['
            Console.Write("\u001b[0m");   // Reset color to default
            Console.Write("Transaction Manager");
            Console.Write("\u001b[31m]\u001b[0m"); // End with green ']'
            Console.Write("\u001b[0m ");   // Reset color to default
            Console.Write(message + "\n");
        }

        public static void Log2(string message)
        {
            if (!IsEnabled2)
            {
                return;
            }

            Console.Write("\u001b[1m\u001b[31m["); // Start with green '['
            Console.Write("\u001b[0m");   // Reset color to default
            Console.Write("Transaction Manager");
            Console.Write("\u001b[31m]\u001b[0m"); // End with green ']'
            Console.Write("\u001b[0m ");   // Reset color to default
            Console.Write(message + "\n");
        }
    }
}

