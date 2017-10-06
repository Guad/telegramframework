using System;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace CoreDumpedTelegramBot.Features
{
    public class Fortune : IBotPlugin
    {
        private string[] _fortune;
        private Random _r = new Random();

        public void Hook(TelegramBotClient bot){}
        public void Update(){}
        public void Stop() {}


        public void Start()
        {
            _fortune = File.ReadAllText("fortunes.txt", Encoding.UTF8).Split('%');
        }

        [Command(Description = "Abre una galleta de la suerte")]
        public async void fortune(Message msg)
        {
            await Program.Client.SendTextMessageAsync(msg.Chat, _fortune[_r.Next(_fortune.Length)],
                replyToMessageId: msg.MessageId);
        }
    }
}