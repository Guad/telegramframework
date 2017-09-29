using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class Tests : IBotPlugin
    {
        public void Hook(TelegramBotClient bot)
        {
        }

        [Command]
        public async void testquery(Message msg)
        {
            var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[]
            {
                new InlineKeyboardCallbackButton("OK",
                    Program.CallbackHandler.AddCallback(async q =>
                    {
                        await Program.Client.AnswerCallbackQueryAsync(q.Id, "You clicked on OK", false);
                        await Program.Client.EditMessageReplyMarkupAsync(q.Message.Chat, q.Message.MessageId, null);
                        return false;
                    })),
                new InlineKeyboardCallbackButton("Cancel",
                    Program.CallbackHandler.AddCallback(async q =>
                    {
                        await Program.Client.AnswerCallbackQueryAsync(q.Id, "You clicked on Cancel", false);
                        await Program.Client.EditMessageReplyMarkupAsync(q.Message.Chat, q.Message.MessageId, null);
                        return false;
                    })),
            });

            Message message = await Program.Client.SendTextMessageAsync(msg.Chat, "Please click on *OK* button.",
                ParseMode.Markdown, replyMarkup: replyMarkup);
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
    }
}