using System;
using System.Diagnostics;

namespace DADTKV.initializer
{
    class Program
    {
        static string GetCurrrentPath()
        {
            string currentDirectory = Directory.GetCurrentDirectory().ToString();

            string[] directorys;
            char sepr;
            if (currentDirectory.Contains('\\'))
            {
                sepr = '\\';
                directorys = currentDirectory.Split('\\');
            }
            else
            {
                sepr = '/';
                directorys = currentDirectory.Split('/');
            }

            string dirHead = "";
            foreach (string directory in directorys)
            {
                if (directory.Equals("Initializer"))
                    break;
                dirHead += directory + sepr;
            }

            return dirHead;
        }

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
            string projectPath = GetCurrrentPath();
            List<string> file_lines = new List<string>(File.ReadAllLines(filePath));

            string timeStart = "";
            int numSlots = 0;
            int timeSlots = 0;

            var LMlist = new List<Tuple<string, string>>();
            var TMlist = new List<Tuple<string, string>>();
            var CLlist = new List<Tuple<string, string>>();
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
                                processes.Add(new DADTransactionManagerProc(projectPath, processId, processArg));
                                TMlist.Add(new Tuple<string, string>(processId, processArg));
                                break;
                            case "L":
                                processes.Add(new DADLeaseManagerProc(projectPath, processId, processArg));
                                LMlist.Add(new Tuple<string, string>(processId, processArg));
                                break;
                            case "C":
                                processes.Add(new DADClientProc(projectPath, processId, processArg));
                                break;
                        }
                        break;
                    case "S":
                        numSlots = int.Parse(components[1]);
                        break;
                    case "T":
                        timeStart = components[1];
                        break;
                    case "D":
                        timeSlots = int.Parse(components[1]);
                        break;
                    case "F":
                        string[] suspicionLog = components;
                        int timeSlot = int.Parse(components[1]);

                        foreach (string suspicion in suspicionLog)
                        {
                            if (suspicion[0] == '(')
                            {
                                string[] suspects = suspicion.Split(',');
                                string suspicious = suspects[0].Remove(0, 1);
                                string suspect = suspects[1].Remove(suspects[1].Length - 1, 1);

                                foreach (DADProcess process in processes)
                                {
                                    if (process is DADManagerProcess && process.Id == suspicious)
                                    {
                                        ((DADManagerProcess)process).AddSusTuple(new Tuple<int, string>(timeSlot, suspect));
                                    }
                                }
                            }
                        }

                        break;
                }
            }

            foreach (var proc in processes)
            {
                switch (proc)
                {
                    case DADManagerProcess MProc:
                        MProc.NumSlots = numSlots;
                        MProc.TimeSlot = timeSlots;
                        MProc.LmsList = LMlist;
                        MProc.TmsList = TMlist;
                        break;

                    case DADClientProc LMproc:
                        LMproc.TmsList = TMlist;
                        break;
                }
                proc.TimeStart = timeStart;
            }

            foreach (var proc in processes)
                Console.WriteLine(proc.GetProcessArgs());
            foreach (var proc in processes)
                Process.Start(proc.GetProcessStartInfo(os));
        }
    }
}
