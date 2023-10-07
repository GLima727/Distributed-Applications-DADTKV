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

    }
}
