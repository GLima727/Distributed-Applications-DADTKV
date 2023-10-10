
namespace DADTKV.transactionManager
{
    class FlagReader
    {

        public static Dictionary<string, Action<string, TransactionManager>> ArgumentActions = new Dictionary<string, Action<string, TransactionManager>>
        {
            {"--id", IdReader},
             {"--timeStart", TimeStartReader},
             {"--tms", TmsReader},
             {"--url", UrlReader},
             {"--timeSlot", TimeSlotReader},
             {"--numSlot", NumSlotReader},
             {"--lms", LmsReader},
             {"--susList", SusListReader},
             {"--roundsDown", RoundsDownReader},
        };

        public static void IdReader(string id, TransactionManager tm)
        {
            tm.Id = id;
        }

        public static void TimeStartReader(string timeStart, TransactionManager tm)
        {
            tm.TimeStart = timeStart;
        }

        public static void TmsReader(string arg, TransactionManager tm)
        {
            List<Tuple<string,string>> Tms = new List<Tuple<string,string>>();
            foreach (string transMan in arg.Split(',')) {
                if (transMan != "") {
                    string[] elems = transMan.Split("%");
                    Tms.Add(new Tuple<string, string>(elems[0], elems[1]));
                }
                
            }
            tm.Tms = Tms;
        }

        public static void UrlReader(string arg, TransactionManager tm)
        {
            Uri uri = new Uri(arg);
            tm.Port = uri.Port;
            tm.Url = uri.Host;
        }

        public static void TimeSlotReader(string arg, TransactionManager tm)
        {
            tm.TimeSlot = int.Parse(arg);
        }

        public static void NumSlotReader(string arg, TransactionManager tm)
        {
            tm.NumSlot = int.Parse(arg);
        }

        public static void LmsReader(string arg, TransactionManager tm)
        {
            List<Tuple<string,string>> Lms = new List<Tuple<string,string>>();
            foreach (string leaseMan in arg.Split(","))
            {
                if (leaseMan != "")
                {
                    string[] elems = leaseMan.Split("%");
                    Lms.Add(new Tuple<string, string>(elems[0], elems[1]));
                }
            }
            tm.Lms = Lms;
        }

        public static void SusListReader(string arg, TransactionManager tm)
        {
            List<Tuple<int,string>> SusList = new List<Tuple<int,string>>();
            foreach(string sus in arg.Split(","))
            {
                if (sus != "")
                {
                    string[] elems = sus.Split("%");
                    SusList.Add(new Tuple<int, string>(int.Parse(elems[0]), elems[1]));
                }
            }
            tm.SusList = SusList;
        }

        public static void RoundsDownReader(string arg, TransactionManager tm)
        {
            List<int> RoundsDown = new List<int>();
            foreach (string elem in arg.Split(','))
            {
                if (elem != "")
                {
                    RoundsDown.Add(int.Parse(elem));
                }
            }
            tm.RoundsDowns = RoundsDown;
        }

    }
}

