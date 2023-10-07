

using System.Diagnostics;

namespace DADTKV.initializer
{
    class Initializer
    {

        static string getCurrrentPath()
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
                dirHead += directory + sepr;
                if (directory.Equals("DadTkvProject"))
                    break;
            }

            return dirHead;

        }

        public static string getLeasersList(List<string> lines)
        {
            string leaderList = "";
            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                string command = components[0];
                if (command == "P")
                {
                    string processId = components[1];
                    string processType = components[2];
                    string processArg = components[3];
                    if (processType == "L")
                        leaderList += processId + " " + processArg + " ";
                }
            }

            return leaderList;
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

            //VARIABLES//
            string projectPath = getCurrrentPath();
            List<string> file_lines = new List<string>(File.ReadAllLines(filePath));
            string LleaderList = getLeasersList(file_lines);
            //-------------------------------------------------------------------------//
            




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
                        ProcessStartInfo startInfo;

                        switch (processType)
                        {
                            case "T":
                                startInfo = new ProcessStartInfo();
                                switch (os.Platform)
                                {
                                    case PlatformID.Win32NT:
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.Arguments =
                                            $"/k cd \"{projectPath + "\\TransactionManager"}\"" +
                                            " && " +
                                            $"dotnet run -- \"{processId}\" \"{processArg}\"";
                                        startInfo.UseShellExecute = true;
                                        break;
                                    case PlatformID.Unix:
                                        startInfo.FileName = "/bin/bash";
                                        startInfo.Arguments =
                                            $"-c \"kgx -e 'cd {projectPath + "TransactionManager"}" +
                                            " && " +
                                            $" dotnet run {processId} {processArg}'\"";
                                        Thread.Sleep(20);
                                        break;
                                }
                                Process.Start(startInfo);
                                break;
                            case "L":
                                startInfo = new ProcessStartInfo();
                                switch (os.Platform)
                                {
                                    case PlatformID.Win32NT:
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.Arguments =
                                            $"/k cd \"{projectPath + "\\LeaseManager"}\"" +
                                            " && " +
                                            $"dotnet run -- \"{processId}\" \"{processArg}\" \"{LleaderList}\"";
                                        startInfo.UseShellExecute = true;
                                        break;
                                    case PlatformID.Unix:
                                        startInfo.FileName = "/bin/bash";
                                        startInfo.Arguments =
                                            $"-c \"kgx -e 'cd {projectPath + "LeaseManager"}" +
                                            " && " +
                                            $" dotnet run {processId} {processArg} {LleaderList}'\"";
                                        Thread.Sleep(200);
                                        break;
                                }
                                Process.Start(startInfo);
                                break;
                            case "C":
                                startInfo = new ProcessStartInfo();
                                switch (os.Platform)
                                {
                                    case PlatformID.Win32NT:
                                        startInfo.FileName = "cmd.exe";
                                        startInfo.Arguments =
                                            $"/k cd \"{projectPath + "\\Client"}\"" +
                                            " && " +
                                            $"dotnet run -- \"{processId}\" \"{projectPath + "/Initializer/scripts/" + processArg}\"";
                                        startInfo.UseShellExecute = true;
                                        break;
                                    case PlatformID.Unix:
                                        startInfo.FileName = "/bin/bash";
                                        break;
                                }
                                Process.Start(startInfo);
                                break;
                        }
                        break;
                }
            }

        }
    }
}