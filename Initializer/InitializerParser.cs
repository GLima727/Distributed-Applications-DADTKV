using System.Diagnostics;

namespace DADTKV.initializer
{
    class InitializerParser
    {
        public static string GetCurrrentPath()
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

        public static string GetLeasersList(List<string> lines)
        {
            string leaserList = "";
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
                        leaserList += processId + " " + processArg + " ";
                }
            }

            return leaserList;
        }

        public static string getLeaseManAddresses(List<string> lines)
        {
            string leaseMans = "";
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
                        leaseMans += processArg + " ";
                }

            }
            leaseMans = leaseMans.TrimEnd();

            return leaseMans;
        }

        public static string getTransManAddresses(List<string> lines)
        {
            string transMans = "";
            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                string command = components[0];
                if (command == "P")
                {
                    string processId = components[1];
                    string processType = components[2];
                    string processArg = components[3];
                    if (processType == "T")
                        transMans += processArg + " ";
                }

            }
            transMans = transMans.TrimEnd();

            return transMans;
        }

        public static string getTransManIds(List<string> lines)
        {
            string transIds = "";
            foreach (string line in lines)
            {
                string[] components = line.Split(' ');
                string command = components[0];
                if (command == "P")
                {
                    string processId = components[1];
                    string processType = components[2];
                    if (processType == "T")
                        transIds += processId + " ";
                }

            }
            transIds = transIds.TrimEnd();

            return transIds;
        }

    }
}
