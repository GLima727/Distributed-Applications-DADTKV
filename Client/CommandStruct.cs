namespace DADTKV.client
{
    /// <summary>
    ///  Defines the behaviour of a command.
    /// </summary>  
    public interface Command
    {
    }

    /// <summary>
    /// Represents a T command, which involves reading and writing data.
    /// </summary>
    public struct TCommand : Command
    {
        /// <summary>
        /// List of string keys to be read.
        /// </summary>
        public List<string> ReadSet;

        /// <summary>
        /// List with keys and values to be written.
        /// </summary>
        public List<(string, int)> WriteSet;

        /// <summary>
        /// Prints the details of the T command.
        /// </summary>
        public override string ToString()
        {
            string s = "Command Type: T read Set<";
            foreach (var key in ReadSet)
            {
                s += $" {key} |";
            }
            s += "> write Set <";
            foreach (var (key, value) in WriteSet)
            {
                s += $" {key} : {value} |";
            }
            return s + '>';
        }
        /// <summary>
        /// Returns the List of objects to read in a List of strings
        /// </summary>
        public List<string> GetReadSet()
        {
            return ReadSet;
        }
        /// <summary>
        /// Returns the List of objects to write in a List of (string,int)
        /// </summary>
        public List<(string, int)> GetWriteSet()
        {
            return WriteSet;
        }
    }

    /// <summary>
    /// Represents a W command, which involves waiting for a specific time.
    /// </summary>
    public struct WCommand : Command
    {
        /// <summary>
        /// Wait time in milliseconds 
        /// </summary>
        public int WaitTime;

        /// <summary>
        /// Prints the details of W the command.
        /// </summary>
        public override string ToString()
        {
            return $"Command Type: W command {WaitTime}";
        }
        /// <summary>
        /// Returns the value of the time in an int
        /// </summary>
        public int GetWaitTime()
        {
            return WaitTime;
        }
    }

    /// <summary>
    /// Represents a S command, which involves broadcasting a request for all TM servers to print their status.
    /// </summary>
    public struct SCommand : Command
    {
        /// <summary>
        /// Prints the details of S the command.
        /// </summary>
        public override string ToString()
        {
            return "Command Type: S command";
        }
    }
}
