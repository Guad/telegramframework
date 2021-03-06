﻿using System;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CoreDumpedTelegramBot.Features
{
    public class Echo : IBotPlugin
    {
        public void Hook(TelegramBotClient bot){}

        [Command("echo", GreedyArg = true, Description = "Repite despues de mi")]
        public async void BotOnOnMessage(Message msg, string text)
        {
            Program.ConsoleLog(string.Format("Mensaje recibido: {0} del usuario: {1}", text, msg.From.Id));
            
            await Program.Client.SendTextMessageAsync(msg.Chat, text, ParseMode.Markdown,
                replyToMessageId: msg.MessageId);
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