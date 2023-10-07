using System.Diagnostics;

namespace DADTKV.initializer
{
    class Program
    {
        public static void Main(string[] args)
        {
            string filePath = args[0];

            if (args.Length < 1)
            {
                Console.WriteLine("Error: Missing arguments.");
                Console.WriteLine("Usage: Initializer.exe <PathToSystemConfigurationFile>");
                return;
            }

            OperatingSystem os = Environment.OSVersion;

            Console.WriteLine("Initializing...");

            // VARIABLES //
            string projectPath = InitializerParser.GetCurrrentPath();
            List<string> file_lines = new List<string>(File.ReadAllLines(filePath));
            string leaserList = InitializerParser.GetLeasersList(file_lines);
            string tmIDs = InitializerParser.getTransManIds(file_lines);
            string tmAdresses = InitializerParser.getTransManAddresses(file_lines);
            string lmAdresses = InitializerParser.getLeaseManAddresses(file_lines);

            //-------------------------------------------------------------------------//


            // Process list
            var processes = new List<DADProcess>();

            // Read input file. 
            foreach (string line in file_lines)
            {
                string[] components = line.Split(' ');
                string command = components[0];
                switch (command)
                {
                    case "P":
                        string processId = components[1];
                        string processType = components[2];
                        string processArg = components[3];

                        switch (processType)
                        {
                            case "T":
                                processes.Add(new DADTransactionManagerProc(projectPath, processId,processArg, tmIDs, tmAdresses, lmAdresses));
                                break;
                            case "L":
                                processes.Add(new DADLeaseManagerProc(projectPath, processId, processArg, tmIDs, tmAdresses, leaserList));
                                break;
                            case "C":
                                processes.Add(new DADClientProc(projectPath, processId,processArg, tmIDs, tmAdresses));
                                break;
                        }
                        break;
                }
            }

        }
    }
}
