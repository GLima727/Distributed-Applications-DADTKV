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
    }
}
