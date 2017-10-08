using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CoreDumpedTelegramBot.Features
{
    public class Stopwatch : IBotPlugin
    {
        private class ChatStopwatches
        {
            public Message TimerMessage;
            public DateTime? Start;
            public object Lock = new object();
            public long LastTicks;
        }

        private ChatData<ChatStopwatches> _data = new ChatData<ChatStopwatches>();

        [Command(Description = "Empezar o parar un reloj")]
        public async void stopwatch(Message msg)
        {
            var ourData = _data[msg.Chat];

            if (ourData.Start.HasValue)
            {
                var span = DateTime.UtcNow.Subtract(ourData.Start.Value);
                ourData.Start = null;
                await Program.Client.SendTextMessageAsync(msg.Chat, string.Format("Timer:\n*{0}*", span), ParseMode.Markdown);
                await Program.Client.DeleteMessageAsync(ourData.TimerMessage.Chat, ourData.TimerMessage.MessageId);

                _data.Remove(msg.Chat);
            }
            else
            {
                ourData.TimerMessage = await Program.Client.SendTextMessageAsync(msg.Chat, string.Format("Timer:\n*{0}*", TimeSpan.FromSeconds(0)), ParseMode.Markdown);
                ourData.Start = DateTime.UtcNow;
            }
        }

        public void Hook(TelegramBotClient bot)
        {

        }

        public async void Update()
        {
            foreach (long key in _data.Keys())
            {
                var ourData = _data[key];
                if (!ourData.Start.HasValue) continue;
                
                var span = DateTime.UtcNow.Subtract(ourData.Start.Value);
                span = TimeSpan.FromSeconds((int)span.TotalSeconds);
                if (ourData.LastTicks != span.Ticks)
                {
                    ourData.LastTicks = span.Ticks;
                    await Program.Client.EditMessageTextAsync(ourData.TimerMessage.Chat, ourData.TimerMessage.MessageId,
                        string.Format("Timer:\n*{0}*", span), ParseMode.Markdown);
                }
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}