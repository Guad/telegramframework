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
        public void Hook(TelegramBotClient bot)
        {
            
        }

        public void Update() {}
        public void Start() {}
        public void Stop() {}

        private bool isSettingupChat;
        private bool isChatGoingOn;
        private Message votingMessage;
        private string votingQuestion;

        private List<int> voters = new List<int>();
        private List<string> options = new List<string>();
        private string[] callbacks;
        private int[] votes;
        private IReplyMarkup mainReplyMarkup;

        private async Task<bool> canDoVotes(Chat chat, int user)
        {
            if (chat.Id == user)
                return true;
            ChatMember[] admins = await Program.Client.GetChatAdministratorsAsync(chat);

            return admins.Any(a => a.User.Id == user);
        }
        
        [Command(Description = "Crear un voto nuevo")]
        public async void newvote(Message msg)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id))
                return;

            if (isChatGoingOn)
            {
                await Program.Client.EditMessageReplyMarkupAsync(votingMessage.Chat, votingMessage.MessageId, null);
                callbacks.ToList().ForEach(s => Program.CallbackHandler.RemoveCallback(s));
                callbacks = null;
            }

            votes = null;
            voters.Clear();
            options.Clear();
            votingMessage = null;
            isChatGoingOn = false;

            await Program.Client.SendTextMessageAsync(msg.Chat, "Vamos a empezar un nuevo voto. Indica la pregunta.");
            isSettingupChat = true;
        }

        [Command(GreedyArg = true, Description = "Añadir una respuesta")]
        public async void addq(Message msg, string text)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || !isSettingupChat)
                return;

            options.Add(text);
            await Program.Client.SendTextMessageAsync(msg.Chat, "Añadido como opcion " + (options.Count - 1) + ". Quitar preguntas con /remq");
        }

        [Command(GreedyArg = true, Description = "Poner la pregunta")]
        public async void setq(Message msg, string question)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || !isSettingupChat)
                return;

            await Program.Client.SendTextMessageAsync(msg.Chat, "Ahora añade respuestas con /addq");
            votingQuestion = question;
        }

        [Command(Description = "Quitar una respuesta")]
        public async void remq(Message msg, int index)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || !isSettingupChat || index < 0 || index >= options.Count)
                return;

            options.RemoveAt(index);
            await Program.Client.SendTextMessageAsync(msg.Chat, "Quitada opcion #" + index + "");
        }

        [Command(Description = "Iniciar el voto")]
        public async void startvote(Message msg)
        {
            if (!await canDoVotes(msg.Chat, msg.From.Id) || !isSettingupChat)
                return;

            List<InlineKeyboardCallbackButton> buttens = new List<InlineKeyboardCallbackButton>();
            callbacks = new string[options.Count];
            votes = new int[options.Count];

            for (int i = 0; i < options.Count; i++)
            {
                var i1 = i;
                buttens.Add(new InlineKeyboardCallbackButton(options[i] + " [0]",
                    callbacks[i] = Program.CallbackHandler.AddCallback(async q =>
                    {
                        if (voters.Contains(q.From.Id))
                        {
                            await Program.Client.AnswerCallbackQueryAsync(q.Id, "You have already voted!", true);
                        }
                        else
                        {
                            votes[i1]++;
                            voters.Add(q.From.Id);
                            await Program.Client.AnswerCallbackQueryAsync(q.Id, "Vote successful!", false);
                            UpdateVotingMessage();
                        }

                        return false;
                    })));
            }

            mainReplyMarkup = new InlineKeyboardMarkup(buttens.Select(b => new InlineKeyboardButton[1] { b }).ToArray());

            string text = string.Format("*VOTE*\n{0}", votingQuestion);
            votingMessage = await Program.Client.SendTextMessageAsync(msg.Chat, text,
                ParseMode.Markdown, replyMarkup: mainReplyMarkup);
            isSettingupChat = false;
        }

        private void UpdateVotingMessage()
        {
            var replyMarkup = (InlineKeyboardMarkup) mainReplyMarkup;
            for (int i = 0; i < replyMarkup.InlineKeyboard.Length; i++)
            {
                replyMarkup.InlineKeyboard[i][0].Text = string.Format("{0} [{1}]", options[i], votes[i]);
            }

            string text = string.Format("*VOTE*: {0}", votingQuestion);
            Program.Client.EditMessageTextAsync(votingMessage.Chat, votingMessage.MessageId, text, ParseMode.Markdown, replyMarkup: replyMarkup);
        }
    }
}