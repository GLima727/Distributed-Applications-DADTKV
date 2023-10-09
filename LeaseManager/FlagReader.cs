namespace DADTKV.leaseManager
{
    class FlagReader
    {
        public static Dictionary<string, Action<string, LeaseManager>> ArgumentActions = new Dictionary<string, Action<string, LeaseManager>>
        {
            {"--id", IdReader},
            {"--url", UrlReader},
            {"--timeSlot", TimeSlotReader},
            {"--numSlot", NumSlotReader},
            {"--roundsDown", RoundsDownReader},
            {"--timeStart", TimeStartReader},
            {"--lms", LmsReader},
            {"--tms", TmsReader},
            {"--susList", SusListReader},
        };

        public static void IdReader(string arg, LeaseManager lm)
        {
            lm.Id = arg;
        }

        public static void UrlReader(string arg, LeaseManager lm)
        {
            int porti = arg.LastIndexOf(':');
            int urli = arg.IndexOf(':');

            if (porti != -1 && urli != -1)
            {
                lm.Url = arg.Substring(urli + 3, porti - urli - 3);
                lm.Port = int.Parse(arg.Substring(porti + 1));
            }
            else
            {
                throw new Exception("The url is invalid!");
            }

            lm.Url = arg;
        }

        public static void TimeSlotReader(string arg, LeaseManager lm)
        {
            lm.TimeSlot = int.Parse(arg);
        }

        public static void NumSlotReader(string arg, LeaseManager lm)
        {
            lm.NumSlot = int.Parse(arg);
        }

        public static void RoundsDownReader(string arg, LeaseManager lm)
        {
            var rd = new List<int>();
            foreach (string elem in arg.Split(','))
            {
                if (elem != "")
                {
                    rd.Add(int.Parse(elem));
                }
            }
            lm.RoundsDowns = rd;
        }

        public static void TimeStartReader(string arg, LeaseManager lm)
        {
            lm.TimeStart = arg;
        }

        public static void LmsReader(string arg, LeaseManager lm)
        {
            var lms = new List<Tuple<string, string>>();
            foreach (string leaseManager in arg.Split(','))
            {
                if (leaseManager != "")
                {
                    var elem = leaseManager.Split('%');
                    lms.Add(new Tuple<string, string>(elem[0], elem[1]));
                }
            }
            lm.Lms = lms;
        }

        public static void TmsReader(string arg, LeaseManager lm)
        {
            var tms = new List<Tuple<string, string>>();
            foreach (string tramsManager in arg.Split(','))
            {
                if (tramsManager != "")
                {
                    var elem = tramsManager.Split('%');
                    tms.Add(new Tuple<string, string>(elem[0], elem[1]));
                }
            }
            lm.Tms = tms;
        }

        public static void SusListReader(string arg, LeaseManager lm)
        {
            var tms = new List<Tuple<int, string>>();

            foreach (string tramsManager in arg.Split(','))
            {
                if (tramsManager != "")
                {
                    var elem = tramsManager.Split('%');
                    tms.Add(new Tuple<int, string>(int.Parse(elem[0]), elem[1]));
                }
            }
            lm.SusList = tms;
        }
    }
}

