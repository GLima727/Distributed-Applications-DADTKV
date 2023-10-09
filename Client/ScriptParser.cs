namespace DADTKV.client
{
    /// <summary>
    /// A static class for parsing script files.
    /// </summary>
    public static class ScriptParser
    {
        /// <summary>
        /// Parses a script file and returns a list of commands.
        /// </summary>
        /// <param name="scriptPath">The file path of the script.</param>
        /// <returns>A list of Command objects.</returns>
        public static List<Command> ParseScript(string scriptPath)
        {
            List<Command> commands = new List<Command>();

            using (StreamReader sr = new StreamReader(scriptPath))
            {
                String? line = sr.ReadLine();
                while (line != null)
                {
                    switch (line[0])
                    {
                        case '#':
                            // Comment line, skip processing.
                            break;
                        case 'W':
                            // Wait command
                            line = line[2..];
                            commands.Add(new WCommand { WaitTime = int.Parse(line) });
                            break;
                        case 'T':
                            // Transaction command
                            TCommand command = new TCommand { };
                            command.ReadSet = new List<string>();
                            command.WriteSet = new List<(string, int)>();

                            line = line[2..];

                            // Extract Read Set 
                            string readSet = line[(line.IndexOf('(') + 1)..line.IndexOf(')')];
                            string[] keys = readSet.Split(',');
                            foreach (string key in keys)
                            {
                                if (key.Length != 0)
                                    command.ReadSet.Add(key);
                            }

                            line = line[(line.IndexOf(')') + 2)..];
                            // Extract Write Set
                            string writeSet = line[(line.IndexOf('(') + 1)..line.IndexOf(')')];
                            while (writeSet.Length != 0)
                            {
                                string pair = writeSet[(writeSet.IndexOf('<') + 1)..((writeSet.IndexOf('>')))];
                                string cleanedPair = pair.Trim(' ', '<', '>');

                                string[] components = cleanedPair.Split(',');
                                command.WriteSet.Add((components[0], int.Parse(components[1])));
                                writeSet = (writeSet[(writeSet.IndexOf('>') + 1)..]);
                            }

                            commands.Add(command);
                            break;
                        default:
                            break;

                    }
                    line = sr.ReadLine();
                }
            }

            return commands;
        }
    }

}
