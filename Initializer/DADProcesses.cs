using System.Diagnostics;


namespace DADTKV.initializer
{
    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public class DADProcess
    {
        private string _projectPath = "";
        public string ProjectPath
        {
            get { return _projectPath; }
            set { _projectPath = value; }
        }

        private string _id = "";
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private ProcessStartInfo _processStartInfo;
        public ProcessStartInfo StartInfo
        {
            get { return _processStartInfo; }
            set { _processStartInfo = value; }
        }

        private string _timeSart = "";
        public string TimeStart
        {
            get { return _timeSart; }
            set { _timeSart = value; }
        }

        private List<Tuple<string, string>> _tmsList = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> TmsList
        {
            get { return _tmsList; }
            set { _tmsList = value; }
        }

        public DADProcess(string projectPath, string id)
        {
            _projectPath = projectPath;
            _id = id;
            _processStartInfo = new ProcessStartInfo();
        }

        public string GetTmsString()
        {
            string res = "";

            foreach (Tuple<string, string> tm in TmsList)
                res += $"{tm.Item1}%{tm.Item2},";

            return res;
        }


        public virtual string GetProcessArgs()
        {
            return $"--id {Id} " +
                $"--tmStart " +
                $"--tms {GetTmsString()} ";
        }
    }

    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public class DADManagerProcess : DADProcess
    {
        public DADManagerProcess(string projectPath, string id, string url) : base(projectPath, id)
        {
            _url = url;
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        private int _numSlots;
        public int NumSlots
        {
            get { return _numSlots; }
            set { _numSlots = value; }
        }

        private List<Tuple<string, string>> _lmsList = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> LmsList
        {
            get { return _lmsList; }
            set { _lmsList = value; }
        }

        private List<Tuple<int, string>> _susList = new List<Tuple<int, string>>();
        public List<Tuple<int, string>> SusList
        {
            get { return _susList; }
            set { _susList = value; }
        }

        public void AddSusTuple(Tuple<int, string> susList)
        {
            _susList.Add(susList);
        }

        private int _timeSlot = 0;
        public int TimeSlot
        {
            get { return _timeSlot; }
            set { _timeSlot = value; }
        }

        private List<int> _roundsDown = new List<int>();
        public List<int> RoundsDown
        {
            get { return _roundsDown; }
            set { _roundsDown = value; }
        }

        public string GetSuspiciousListString()
        {
            string res = "";

            foreach (Tuple<int, string> sus in this.SusList)
                res += $"{sus.Item1}%{sus.Item2},";

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
                res += $"{peer.Item1}%{peer.Item2},";

            return res;
        }

        public override string GetProcessArgs()
        {
            return base.GetProcessArgs() +
                $"--url {Url} " +
                $"--timeSlot {TimeSlot} " +
                $"--numSlot {NumSlots} " +
                $"--lms {GetLmsString()} " +
                $"--susList {GetSuspiciousListString()} " +
                $"--roundsDown {GetRoundsDownString()} ";
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
                $"--script {script} ";
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

