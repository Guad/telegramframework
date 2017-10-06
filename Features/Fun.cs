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

        [Command(Description = "Calcular una simple expresion matematica", GreedyArg = true)]
        public async void calc(Message msg, string expresion)
        {
            float result = 0;

            //try
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
            //catch (Exception)
            {
                //throw;
            } // Fuck you
        }
    }
}