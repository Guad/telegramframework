using System;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CoreDumpedTelegramBot.Features
{
    public class Fun : IBotPlugin
    {
        private Random _rng = new Random();

        public void Hook(TelegramBotClient bot)
        {
        }

        public void Update()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        [Command(Description = "Tirar una moneda")]
        public async void flip(Message msg)
        {
            await Program.Client.SendTextMessageAsync(msg.Chat,
                string.Format("La moneda ha caído en *{0}*!", _rng.Next(2) == 1 ? "cara" : "cruz"), ParseMode.Markdown, replyToMessageId: msg.MessageId);
        }

        [Command(Description = "Tirar un dado")]
        public async void dado(Message msg, int caras = 6)
        {
            int result = _rng.Next(1, caras + 1);
            await Program.Client.SendTextMessageAsync(msg.Chat,
                string.Format("El dado ha caído en *{0}*!", result), ParseMode.Markdown, replyToMessageId: msg.MessageId);
        }

        [Command(Description = "I'd just like to interject for a moment")]
        public async void interject(Message msg, string linux, string gnu)
        {
            string interjection = @"I'd just like to interject for a moment. What you're refering to as {0}, is in fact, {1}/{0}, or as I've recently taken to calling it, {1} plus {0}. {0} is not an operating system unto itself, but rather another free component of a fully functioning {1} system made useful by the {1} corelibs, shell utilities and vital system components comprising a full OS as defined by POSIX.

Many computer users run a modified version of the {1} system every day, without realizing it. Through a peculiar turn of events, the version of {1} which is widely used today is often called {0}, and many of its users are not aware that it is basically the {1} system, developed by the {1} Project.

There really is a {0}, and these people are using it, but it is just a part of the system they use. {0} is the kernel: the program in the system that allocates the machine's resources to the other programs that you run. The kernel is an essential part of an operating system, but useless by itself; it can only function in the context of a complete operating system. {0} is normally used in combination with the {1} operating system: the whole system is basically {1} with {0} added, or {1}/{0}. All the so-called {0} distributions are really distributions of {1}/{0}!";

            if (msg.Chat.Type != ChatType.Private && await Voting.IsGroupAdministrator(msg.Chat, Program.BotPersona.Id))
                await Program.Client.DeleteMessageAsync(msg.Chat, msg.MessageId);

            await Program.Client.SendTextMessageAsync(msg.Chat, string.Format(interjection, linux, gnu));
        }

        [Command(Description = "Deja que el bot elija por ti. Separa las cosas con ;", GreedyArg = true, Alias = "choose")]
        public async void elige(Message msg, string lista)
        {
            string[] options = lista.Split(';');

            await Program.Client.SendTextMessageAsync(msg.Chat,
                string.Format("Esto suena bien:\n*{0}*", options[_rng.Next(options.Length)]), ParseMode.Markdown, replyToMessageId: msg.MessageId);
        }

        [Command(Description = "Calcular una simple expresion matematica", GreedyArg = true)]
        public async void calc(Message msg, string expresion)
        {
            float result = 0;

            try
            {
                expresion = expresion.Replace(" ", "");

                int i = 0;
                while (char.IsDigit(expresion[i]) || expresion[i] == '.')
                    i++;

                float firstOperand = float.Parse(expresion.Substring(0, i), CultureInfo.InvariantCulture);

                char op = expresion[i++];
                float secondOperand = float.Parse(expresion.Substring(i));

                switch (op)
                {
                    case '+':
                        result = firstOperand + secondOperand;
                        break;
                    case '-':
                        result = firstOperand - secondOperand;
                        break;
                    case '*':
                        result = firstOperand * secondOperand;
                        break;
                    case '/':
                        result = firstOperand / secondOperand;
                        break;
                }


                await Program.Client.SendTextMessageAsync(msg.Chat,
                    string.Format("El resultado es *{0}*!", result), ParseMode.Markdown,
                    replyToMessageId: msg.MessageId);
            }
            catch (Exception)
            {
            } // Fuck you
        }
    }
}