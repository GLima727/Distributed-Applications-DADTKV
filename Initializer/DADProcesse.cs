using System;
using System.Diagnostics;


namespace DADTKV.initializer
{
    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public interface DADProcess
    {
        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os);
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
        private string tmUrls;
        private string tmIds;
        public ProcessStartInfo startInfo;

        public DADLeaseManagerProc(string projectPath, string id, string url,string tmIds, string tmUrls, string peers)
        {
            this.projectPath = projectPath;
            this.id = id;
            this.url = url;
            this.tmIds = tmIds;
            this.tmUrls = tmUrls;
            this.peers = peers;
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

        private string GetTmsIdsString()
        {
            return tmIds;
        }

        private string GetTmsUrlsString()
        {
            return tmUrls;
        }

        private string GetProcessArgs()
        {
            return $"--id {id}" +
                $"--url {url}" +
                $"--timeSlot {timeSlot}" +
                $"--peers {peers}" +
                $"--susList {GetSuspiciousListString()}" +
                $"--roundsDown {GetRoundsDownString()}" +
                $"--tmIds {GetTmsIdsString()}" +
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
    /// Represents a Transaction Manager process.
    /// </summary>
     public class DADTransactionManagerProc : DADProcess
    {
        private string projectPath = "";
        private string id = "";
        private string url = "";
        private List<Tuple<int, string>> suspiciousList = new List<Tuple<int, string>>();
        private int timeSlot = 0;
        private string tmIds = "";
        private string tmUrls = "";
        private string lmUrls = "";
        public ProcessStartInfo startInfo;


        public DADTransactionManagerProc(string projectPath, string id, string url, string tmIds, string tmUrls, string lmUrls)
        {
            this.projectPath = projectPath;
            this.id = id;
            this.url = url;
            this.tmIds = tmIds;
            this.tmUrls = tmUrls;
            this.lmUrls = lmUrls;
            startInfo = new ProcessStartInfo();
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
        private string GetTmsIdsString()
        {
            return tmIds;
        }

        private string GetTmsUrlsString()
        {
            return tmUrls;
        }

        private string GetLmsUrlsString()
        {
            return lmUrls;
        }

        private string GetProcessArgs()
        {
            return $"--id {id}" +
                $"--url {url}" +
                $"--timeSlot {timeSlot}" +
                $"--susList {GetSuspiciousListString()}" +
                $"--tmIds {GetTmsIdsString()}" +
                $"--tmUrls {GetTmsUrlsString()}" +
                $"--lmUrls {GetLmsUrlsString()}" +
                "";
        }

        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os)
        {

            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments =
                        $"/k cd \"{projectPath + "\\TransactionManager"}\"" +
                        " && " +
                        "dotnet run " + GetProcessArgs();
                    startInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    startInfo.FileName = "/bin/bash";
                    startInfo.Arguments =
                        $"-c \"kgx -e 'cd {projectPath + "TransactionManager"}" +
                        " && " +
                        "dotnet run " + GetProcessArgs() + "'\"";
                    break;
            }

            return startInfo;
        }
    }
    

    /// <summary>
    /// Represents a Client process.
    /// </summary>
    public class DADClientProc : DADProcess
    {
        private string projectPath = "";
        private string id = "";
        private string script = "";
        private string tmIDs;
        private string tmUrls;
        public ProcessStartInfo startInfo;

        public DADClientProc(string projectPath, string id, string script, string tmIDs, string tmUrls)
        {
            this.projectPath = projectPath;
            this.id = id;
            this.script = script;
            this.tmIDs = tmIDs;
            this.tmUrls = tmUrls;
            startInfo = new ProcessStartInfo();
            this.tmIDs = tmIDs;
        }
        private string GetTmsUrlsString()
        {
            return tmUrls;
        }

        private string GetTmsIdsString()
        {
            return tmIDs;
        }

        private string GetProcessArgs()
        {
            return $"--id {id}" +
                $"--script {script}" +
                $"--tmIDs {GetTmsIdsString()}" +
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
                        $"/k cd \"{projectPath + "\\Client"}\"" +
                        " && " +
                        "dotnet run " + GetProcessArgs();
                    startInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    startInfo.FileName = "/bin/bash";
                    startInfo.Arguments =
                        $"-c \"kgx -e 'cd {projectPath + "Client"}" +
                        " && " +
                        "dotnet run " + GetProcessArgs() + "'\"";
                    break;
            }

            return startInfo;
        }
    }

}

