using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class Voting : IBotPlugin
    {
        private ChatData<VotingData> Data = new ChatData<VotingData>();

        private class VotingData
        {
            public bool IsVoteActive;
            public User BeingSetUpBy;
            public Message VotingMessage;
            public string VotingQuestion;
            public DateTime? Limit;
            public int[] Votes;
            public string[] Callbacks;
            public IReplyMarkup MainReplyMarkup;
            public List<int> Voters = new List<int>();
            public List<string> Options = new List<string>();
        }


        public void Update() {}
        public void Start() {}
        public void Stop() {}
        public void Hook(TelegramBotClient bot)
        {
            bot.OnMessage += BotOnOnMessage;
        }

        private void BotOnOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            var msg = messageEventArgs.Message;
            if (msg == null) return;
            VotingData ourData = Data[msg.Chat];
            if (ourData.BeingSetUpBy?.Id != msg.From.Id) return;


        }

        private async Task<bool> canDoVotes(Chat chat, int user)
        {
            if (chat.Id == user || chat.AllMembersAreAdministrators)
                return true;
            ChatMember[] admins = await Program.Client.GetChatAdministratorsAsync(chat);

            return admins.Any(a => a.User.Id == user);
        }
        
        [Command(Description = "Crear un voto nuevo")]
        public async void newvote(Message msg)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id))
                return;

            VotingData ourData = Data[msg.Chat];

            if (ourData.IsVoteActive)
            {
                await Program.Client.EditMessageReplyMarkupAsync(ourData.VotingMessage.Chat, ourData.VotingMessage.MessageId, null);
                ourData.Callbacks.ToList().ForEach(s => Program.CallbackHandler.RemoveCallback(s));
            }

            ourData = Data[msg.Chat] = new VotingData();
            ourData.BeingSetUpBy = msg.From;

            await Program.Client.SendTextMessageAsync(msg.Chat, "Vamos a empezar una nueva votación. Indica la pregunta con /setq.");
        }

        [Command(GreedyArg = true, Description = "Añadir una respuesta")]
        public async void addq(Message msg, string text)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || Data[msg.Chat].BeingSetUpBy?.Id != msg.From.Id)
                return;

            Data[msg.Chat].Options.Add(text);
            await Program.Client.SendTextMessageAsync(msg.Chat,(Data[msg.Chat].Options.Count - 1) + ". Ha sido añadido como opción." + "Quitar preguntas con /remq");
        }

        [Command(GreedyArg = true, Description = "Poner la pregunta")]
        public async void setq(Message msg, string question)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || Data[msg.Chat].BeingSetUpBy?.Id != msg.From.Id)
                return;

            await Program.Client.SendTextMessageAsync(msg.Chat, "Ahora añade opciones con /addq");
            Data[msg.Chat].VotingQuestion = question;
        }

        [Command(Description = "Quitar una respuesta")]
        public async void remq(Message msg, int index)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || Data[msg.Chat].BeingSetUpBy?.Id != msg.From.Id || index < 0 || index >= Data[msg.Chat].Options.Count)
                return;

            Data[msg.Chat].Options.RemoveAt(index);
            await Program.Client.SendTextMessageAsync(msg.Chat, "La opción #" + index + "ha sido quitada.");
        }

        [Command(Description = "Iniciar el voto")]
        public async void startvote(Message msg)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || Data[msg.Chat].BeingSetUpBy?.Id != msg.From.Id)
                return;

            var ourData = Data[msg.Chat];

            List<InlineKeyboardCallbackButton> buttens = new List<InlineKeyboardCallbackButton>();
            ourData.Callbacks = new string[ourData.Options.Count];
            ourData.Votes = new int[ourData.Options.Count];

            for (int i = 0; i < ourData.Options.Count; i++)
            {
                var i1 = i;
                buttens.Add(new InlineKeyboardCallbackButton(ourData.Options[i] + " [0]",
                    ourData.Callbacks[i] = Program.CallbackHandler.AddCallback(async q =>
                    {
                        if (ourData.Voters.Contains(q.From.Id))
                        {
                            await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Ya has votado antes!", true);
                        }
                        else
                        {
                            ourData.Votes[i1]++;
                            ourData.Voters.Add(q.From.Id);
                            await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Has votado satisfactoriamente!", false);
                            UpdateVotingMessage(ourData);
                        }

                        return false;
                    })));
            }

            ourData.MainReplyMarkup = new InlineKeyboardMarkup(buttens.Select(b => new InlineKeyboardButton[1] { b }).ToArray());

            string text = string.Format("*VOTE*\n{0}", ourData.VotingQuestion);
            ourData.VotingMessage = await Program.Client.SendTextMessageAsync(msg.Chat, text,
                ParseMode.Markdown, replyMarkup: ourData.MainReplyMarkup);
            ourData.BeingSetUpBy = null;
        }

        private void UpdateVotingMessage(VotingData data)
        {
            var replyMarkup = (InlineKeyboardMarkup) data.MainReplyMarkup;
            for (int i = 0; i < replyMarkup.InlineKeyboard.Length; i++)
            {
                replyMarkup.InlineKeyboard[i][0].Text = string.Format("{0} [{1}]", data.Options[i], data.Votes[i]);
            }

            string text = string.Format("*VOTE*\n{0}", data.VotingQuestion);
            Program.Client.EditMessageTextAsync(data.VotingMessage.Chat, data.VotingMessage.MessageId, text, ParseMode.Markdown, replyMarkup: replyMarkup);
        }
    }
}