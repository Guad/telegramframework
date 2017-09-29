using System;

namespace CoreDumpedTelegramBot
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : System.Attribute
    {
        public readonly string CommandString;
        public readonly string CommandHelpText;
        public bool GreedyArg { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }

        public CommandAttribute(string command)
        {
            CommandString = command.TrimStart('/');
            CommandHelpText = null;
        }
        public CommandAttribute(string command, string helpText)
        {
            CommandString = command.TrimStart('/');
            CommandHelpText = helpText;
        }

        public CommandAttribute()
        {
            CommandString = null;
            CommandHelpText = null;
        }
    }
}