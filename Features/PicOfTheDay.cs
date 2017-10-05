using System;
using System.IO;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot.Features
{
    public class PicOfTheDay : IBotPlugin
    {
        private const string BingApiUri = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";

        [Command("potd", Description = "La imagen del dia por parte de Bing")]
        public async void BingImageCommand(Message msg)
        {
            using (var wc = new System.Net.Http.HttpClient())
            {
                string rawjson = await wc.GetStringAsync(BingApiUri);
                dynamic json = JsonConvert.DeserializeObject(rawjson);

                string imguri = "https://www.bing.com" + json.images[0].url;
                string copyright = json.images[0].copyright;

                using (Stream fileStream = await wc.GetStreamAsync(imguri))
                {
                    await Program.Client.SendPhotoAsync(msg.Chat, fileStream.ToFileToSend("bing.png"), copyright);
                }
            }
        }

        public void Hook(TelegramBotClient bot) {}
        public void Update() {}
        public void Start() {}
        public void Stop() {}
    }
}