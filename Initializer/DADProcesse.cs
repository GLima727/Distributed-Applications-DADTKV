using System.Diagnostics;


namespace DADTKV.initializer
{
    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public interface DADProcess
    {
        public void SetTimeSlot(int timeSlot);
    }

    /// <summary>
    /// Represents a Lease Manager process.
    /// </summary>
    public class DADLeaseManagerProc : DADProcess
    {
        private string projectPath = "";
        private string id = "";
        private string url = "";
        private string peers = "";
        private List<Tuple<int, string>> suspiciousList = new List<Tuple<int, string>>();
        private int timeSlot = 0;
        private List<int> roundsDown = new List<int>();
        private List<string> tmUrls = new List<string>(); 
        public ProcessStartInfo startInfo;

        public DADLeaseManagerProc(string projectPath, string id, string url)
        {
            this.projectPath = projectPath;
            this.id = id;
            this.url = url;
            startInfo = new ProcessStartInfo();
        }

        public void SetLeaseManagerPeers(string peers)
        {
            this.peers = peers;
        }

        public void SetTimeSlot(int timeSlot)
        {
            this.timeSlot = timeSlot;
        }

        public void SetSuspiciousList(List<Tuple<int, string>> suspiciousList)
        {
            this.suspiciousList = suspiciousList;
        }

        private string GetSuspiciousListString()
        {
            return "";
        }

        public void SetRoundsDown(List<int> roundsDown)
        {
            this.roundsDown = roundsDown;
        }

        private string GetRoundsDownString()
        {
            return "";
        }

        private string GetTmsUrlsString()
        {
            return "";
        }

        private string GetProcessArgs()
        {
            return $"--id {id}" +
                $"--url {url}" +
                $"--timeSlot {timeSlot}" +
                $"--peers {peers}" +
                $"--susList {GetSuspiciousListString()}" +
                $"--roundsDown {GetRoundsDownString()}" +
                $"--tmUrls {GetTmsUrlsString()}" +
                "";
        }

        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os)
        {

            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments =
                        $"/k cd \"{projectPath + "\\LeaseManager"}\"" +
                        " && " +
                        "dotnet run " + GetProcessArgs();
                    startInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    startInfo.FileName = "/bin/bash";
                    startInfo.Arguments =
                        $"-c \"kgx -e 'cd {projectPath + "LeaseManager"}" +
                        " && " +
                        "dotnet run " + GetProcessArgs() + "'\"";
                    break;
            }

            return startInfo;
        }

    }

    /// <summary>
    /// Represents a Lease Manager process.
    /// </summary>
    /*
     public class DADLeaseManagerProc : DADProcess
    {
    }
    */

    /// <summary>
    /// Represents a Client process.
    /// </summary>
    /*
    public class DADClientProc : DADProcess
    {
    }
    */
}

