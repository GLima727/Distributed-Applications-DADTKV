using Grpc.Core;

class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: Missing arguments.");
            Console.WriteLine("Usage: TransactionManager.exe <TransactionManagerID> <Url>");
            return;
        }
        //System.Diagnostics.Debugger.Launch();

        string id = args[0];
        string processUrl = args[1];

        Uri uri = new Uri(processUrl);

        int port = uri.Port;
        string hostname = uri.Host;

        string startupMessage = "Insecure TransactionManager server listening on port " + port;

        Console.WriteLine(startupMessage);

        ServerPort serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);

        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        Server server = new Server
        {
            Services = { },
            Ports = { serverPort }
        };

        server.Start();

        while (true)
        {

        }


    }
}

