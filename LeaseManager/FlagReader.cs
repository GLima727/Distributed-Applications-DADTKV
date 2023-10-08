namespace DADTKV.leaseManager
{
    class FlagReader
    {

        /*
         * --id lease3 --timeStart 12:10:15 --tms LeaseManager1%http://1.2.3.5:10004,LM2%http://1.2.3.5:10008,lease3%http://1.2.3.5:10009, --url http://1.2.3.5:10009 --timeSlot 10000 --numSlot 100 --lms TM1%http://1.2.3.4:10000,TM2%http://1.2.3.4:10002,TM3%http://1.2.3.4:10003, --susList 1%LM2, --roundsDown
            */

        public static Dictionary<string, Action<string, LeaseManager>> ArgumentActions = new Dictionary<string, Action<string, LeaseManager>>
        {
            {"--id", IdReader},
            // {"--timeStart", TimeStartReader},
            // {"--tms", TmsReader},
            // {"--url", UrlReader},
            // {"--timeSlot", TimeSlotReader},
            // {"--numSlot", NumSlotReader},
            // {"--lms", LmsReader},
            // {"--susList", SusListReader},
            // {"--roundsDown", RoundsDownReader},
        };

        public static void IdReader(string id, LeaseManager tm)
        {
            tm.Id = id;
        }
    }
}

