using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace CoreDumpedTelegramBot.Features
{
    public class Fortune : IBotPlugin
    {
        public void Hook(TelegramBotClient bot){}
        public void Update(){}
        public void Start()
        {
        }

        public void Stop() {}

        [Command(Description = "Abre una galleta de la suerte")]
        public async void fortune(Message msg)
        {
            using (var wc = new HttpClient())
            {
                var rawJson = await wc.GetStringAsync("http://yerkee.com/api/fortune");
                dynamic json = JsonConvert.DeserializeObject(rawJson);
                string fortune = json.fortune;
                await Program.Client.SendTextMessageAsync(msg.Chat, fortune, replyToMessageId: msg.MessageId);
            }
        }
    }
}