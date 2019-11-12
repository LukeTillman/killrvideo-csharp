namespace KillrVideo.Configuration
{
    /// <summary>
    /// Wrapper class to represent arguments passed via the commandline to the host program.
    /// </summary>
    public class CommandLineArgs
    {
        public string[] Args { get; }

        public CommandLineArgs(string[] args)
        {
            Args = args;
        }
    }
}
