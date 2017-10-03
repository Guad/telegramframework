using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot
{
    public class CommandParser
    {
        public IBotPlugin Father;
        public string Command;
        public string Usage;
        public string[] Aliases;
        public bool Greedy;
        public MethodInfo Method;
        public ParameterInfo[] Parameters;
        public string Description;

        private string GetHelpText()
        {
            string helpText;

            if (Parameters.Length > 1)
            {
                int paramCounter = 0;
                helpText = " [" +
                           Parameters.Skip(1)
                               .Select(param => param.IsOptional ? param.Name + "?" : param.Name)
                               .Aggregate((prev, next) => prev + (paramCounter++ == 0 ? "]" : "") + " [" + next + "]") +
                           (Parameters.Length == 2 ? "]" : "");
            }
            else
                helpText = "";

            return helpText;
        }

        public async Task<bool> Parse(Message msg)
        {
            string cmdraw = msg.Text;

            if (string.IsNullOrWhiteSpace(cmdraw)) return false;

            cmdraw = cmdraw.TrimEnd();
            string[] args = cmdraw.Split();
            string ourcmd = args[0].TrimStart('/').ToLower();

            if (ourcmd.Contains("@"))
            {
                string[] at = ourcmd.Split('@');
                ourcmd = at[0];

                if (at.Length == 0 || at[1] != Program.BotPersona.Username.ToLower())
                    return false;
            }

            if (ourcmd != Command.ToLower() &&
                (Aliases == null || Aliases.All(a => a.ToLower() != ourcmd)))
                return false;

            string helpText = "USAGE: /" + ourcmd + GetHelpText();

            int optionalArguments = Parameters.Skip(1).Count(p => p.IsOptional);

            if (args.Length < (Parameters.Length - optionalArguments) || (args.Length > Parameters.Length && !Greedy))
            {
                await Program.Client.SendTextMessageAsync(msg.Chat, helpText, replyToMessageId: msg.MessageId);
                return true;
            }

            object[] arguments = new object[Parameters.Length];
            arguments[0] = msg;

            for (int i = 1; i < Parameters.Length; i++)
            {
                if (args.Length <= i)
                {
                    arguments[i] = Type.Missing;
                    continue;
                }

                if (i == Parameters.Length - 1 && Greedy)
                {
                    arguments[i] = string.Join(" ", args.Skip(i));
                    break;
                }

                try
                {
                    arguments[i] = Convert.ChangeType(args[i], Parameters[i].ParameterType, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    string typeName = Parameters[i].ParameterType.ToString();
                    int dot;
                    if ((dot = typeName.LastIndexOf('.')) != -1)
                        typeName = typeName.Substring(dot + 1);

                    await Program.Client.SendTextMessageAsync(msg.Chat,
                        "Tipo de dato incorrecto para parametro \"" +
                        Parameters[i].Name +
                        "\". Se esperaba " + 
                        typeName
                        , replyToMessageId: msg.MessageId);
                    return true;
                }
            }

            try
            {
                await Task.Run(() => Method.Invoke(Father, arguments));
            }
            catch (Exception e)
            {
                Program.HandleException(e);
                return true;
            }

            return true;
        }

        public override string ToString()
        {
            return string.Format("/{0}{1} {2}{3}",
                Command,
                GetHelpText(),
                Description == null ? "" : "- " + Description,
                Aliases.Length > 0 ? "(Aliases: " + string.Join(", ", Aliases) + ")" : "");
        }

        public string ToSyntaxString()
        {
            string helpText = GetHelpText();
            return string.Format("{0} -{1}{2}", Command, string.IsNullOrEmpty(helpText) ? " " : helpText + ": ", Description);
        }
    }
}