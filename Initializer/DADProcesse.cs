using System.Diagnostics;


namespace DADTKV.initializer
{
    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public class DADProcess
    {
        public DADProcess(string projectPath, string id)
        {
            this.ProjectPath = projectPath;
            this.Id = id;
            StartInfo = new ProcessStartInfo();
        }

        public ProcessStartInfo StartInfo
        {
            get { return StartInfo; }
            set { StartInfo = value; }
        }

        public string ProjectPath
        {
            get { return ProjectPath; }
            set { ProjectPath = value; }
        }
        public string Id
        {
            get { return Id; }
            set { Id = value; }
        }
        public int TimeStart
        {
            get { return TimeStart; }
            set { TimeStart = value; }
        }

        public List<Tuple<string, string>> TmsList
        {
            get { return TmsList; }
            set { TmsList = value; }
        }

        public string GetTmsString()
        {
            string res = "";

            foreach (Tuple<string, string> tm in TmsList)
                res += $"{tm.Item1}%{tm.Item2}";

            return res;
        }


        public virtual string GetProcessArgs()
        {
            return $"--id {Id}" +
                $"--tmStart" +
                $"--tms {GetTmsString()}";
        }
    }

    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public class DADManagerProcess : DADProcess
    {
        public DADManagerProcess(string projectPath, string id, string url) : base(projectPath, id)
        {
            this.Url = url;
        }
        public string Url
        {
            get { return Url; }
            set { Url = value; }
        }
        public int NumSlots
        {
            get { return NumSlots; }
            set { NumSlots = value; }
        }
        public List<Tuple<string, string>> LmsList
        {
            get { return LmsList; }
            set { LmsList = value; }
        }
        public List<Tuple<int, string>> SusList
        {
            get { return SusList; }
            set { SusList = value; }
        }
        public int TimeSlot
        {
            get { return TimeSlot; }
            set { TimeSlot = value; }
        }
        public List<int> RoundsDown
        {
            get { return RoundsDown; }
            set { RoundsDown = value; }
        }

        public string GetSuspiciousListString()
        {
            string res = "";

            foreach (Tuple<int, string> sus in this.SusList)
                res += $"{sus.Item1}%{sus.Item2}";

            return res;
        }

        public string GetRoundsDownString()
        {
            string res = "";

            foreach (int roundDown in RoundsDown)
                res += $"{roundDown},";

            return res;
        }

        public string GetLmsString()
        {
            string res = "";

            foreach (Tuple<string, string> peer in LmsList)
                res += $"{peer.Item1}%{peer.Item2}";

            return res;
        }

        public override string GetProcessArgs()
        {
            return base.GetProcessArgs() +
                $"--url {Url}" +
                $"--timeSlot {TimeSlot}" +
                $"--numSlot {NumSlots}" +
                $"--lms {GetLmsString()}" +
                $"--susList {GetSuspiciousListString()}" +
                $"--roundsDown {GetRoundsDownString()}";
        }
    }

    /// <summary>
    /// Represents a Lease Manager process.
    /// </summary>
    public class DADLeaseManagerProc : DADManagerProcess
    {

        public DADLeaseManagerProc(string projectPath, string id, string url) : base(projectPath, id, url) { }

        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os)
        {
            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    StartInfo.FileName = "cmd.exe";
                    StartInfo.Arguments =
                        $"/k cd \"{ProjectPath + "\\LeaseManager"}\"" +
                        " && " +
                        "dotnet run " + base.GetProcessArgs();
                    StartInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    StartInfo.FileName = "/bin/bash";
                    StartInfo.Arguments =
                        $"-c \"kgx -e 'cd {ProjectPath + "LeaseManager"}" +
                        " && " +
                        "dotnet run " + GetProcessArgs() + "'\"";
                    break;
            }
            return StartInfo;
        }
    }

    /// <summary>
    /// Represents a Transaction Manager process.
    /// </summary>
    public class DADTransactionManagerProc : DADManagerProcess
    {
        public DADTransactionManagerProc(string projectPath, string id, string url) : base(projectPath, id, url) { }

        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os)
        {
            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    StartInfo.FileName = "cmd.exe";
                    StartInfo.Arguments =
                        $"/k cd \"{ProjectPath + "\\TransactionManager"}\"" +
                        " && " +
                        "dotnet run " + base.GetProcessArgs();
                    StartInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    StartInfo.FileName = "/bin/bash";
                    StartInfo.Arguments =
                        $"-c \"kgx -e 'cd {ProjectPath + "TransactionManager"}" +
                        " && " +
                        "dotnet run " + base.GetProcessArgs() + "'\"";
                    break;
            }

            return StartInfo;
        }
    }


    /// <summary>
    /// Represents a Client process.
    /// </summary>
    public class DADClientProc : DADProcess
    {
        public string script = "";

        public DADClientProc(string projectPath, string id, string script) : base(projectPath, id)
        {
            this.script = script;
        }

        public override string GetProcessArgs()
        {
            return base.GetProcessArgs() +
                $"--script {script}";
        }

        public ProcessStartInfo GetProcessStartInfo(OperatingSystem os)
        {

            switch (os.Platform)
            {
                case PlatformID.Win32NT:
                    StartInfo.FileName = "cmd.exe";
                    StartInfo.Arguments =
                        $"/k cd \"{ProjectPath + "\\Client"}\"" +
                        " && " +
                        "dotnet run " + GetProcessArgs();
                    StartInfo.UseShellExecute = true;
                    break;
                case PlatformID.Unix:
                    StartInfo.FileName = "/bin/bash";
                    StartInfo.Arguments =
                        $"-c \"kgx -e 'cd {ProjectPath + "Client"}" +
                        " && " +
                        "dotnet run " + GetProcessArgs() + "'\"";
                    break;
            }

            return StartInfo;
        }
    }

}

