using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot
{
    public class CommandHandler
    {
        public CommandParser[] Commands;

        public void Initialize(IEnumerable<IBotPlugin> plugins)
        {
            List<CommandParser> parsers = new List<CommandParser>();

            foreach (IBotPlugin plugin in plugins)
            {
                Type t = plugin.GetType();
                MethodInfo[] methods = t.GetMethods();

                for (int i = 0; i < methods.Length; i++)
                {
                    CommandAttribute attr;
                    if ((attr = methods[i].GetCustomAttribute<CommandAttribute>()) != null)
                    {
                        CommandParser parser = new CommandParser();

                        parser.Father = plugin;
                        parser.Method = methods[i];
                        parser.Command = (string.IsNullOrWhiteSpace(attr.CommandString)) ? methods[i].Name.ToLower() : attr.CommandString;
                        parser.Greedy = attr.GreedyArg;
                        parser.Parameters = methods[i].GetParameters();
                        parser.Usage = attr.CommandHelpText;
                        parser.Description = attr.Description;
                        parser.Aliases = attr.Alias != null ? attr.Alias.Split(',').ToArray() : new string[0];

                        parsers.Add(parser);
                    }
                }
            }

            Commands = parsers.ToArray();
        }

        public async void Handle(Message msg)
        {
            for (int i = 0; i < Commands.Length; i++)
            {
                if (await Commands[i].Parse(msg))
                    return;
            }
        }
    }
}