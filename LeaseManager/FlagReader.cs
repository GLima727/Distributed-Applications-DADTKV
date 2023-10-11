namespace DADTKV.leaseManager
{
    /// <summary>
    /// Helper class for reading and processing command line arguments for LeaseManager.
    /// </summary>
    class FlagReader
    {
        /// <summary>
        /// Dictionary of argument actions mapped to their respective command-line flags.
        /// </summary>
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

        /// <summary>
        /// Sets the ID for the Lease Manager.
        /// </summary>
        /// <param name="arg">The argument value.</param>
        /// <param name="lm">The Lease Manager instance.</param>
        public static void IdReader(string arg, LeaseManager lm)
        {
            lm.Id = arg;
        }

        // ... (similar comments for other methods)

        /// <summary>
        /// Reads and processes the list of Transmission Managers (TMs).
        /// </summary>
        /// <param name="arg">The argument value.</param>
        /// <param name="lm">The Lease Manager instance.</param>
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

        /// <summary>
        /// Reads and processes the list of suspended leases.
        /// </summary>
        /// <param name="arg">The argument value.</param>
        /// <param name="lm">The Lease Manager instance.</param>
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

