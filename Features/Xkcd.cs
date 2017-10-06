using System;
using System.Net.Http;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot.Features
{
    public class Xkcd : IBotPlugin
    {
        private const string ApiString = "https://xkcd.com/{0}/info.0.json";
        private Random _rng = new Random();
        private int _lastComic = 0;

        public void Hook(TelegramBotClient bot)
        {
        }

        public void Update()
        {
        }

        public async void Start()
        {
            using (var wc = new HttpClient())
            {
                var rawJson = await wc.GetStringAsync(@"https://xkcd.com/info.0.json");
                dynamic json = JsonConvert.DeserializeObject(rawJson);
                _lastComic = json.num;
            }
        }

        public void Stop()
        {
        }

        [Command(Description = "Poner un comic xkcd. Si no se especifica num, se pone uno aleatorio.")]
        public async void xkcd(Message msg, int num = -1)
        {
            int comicNum = num;
            if (num <= 0 || num > _lastComic)
                comicNum = _rng.Next(_lastComic) + 1;

            try
            {
                using (var wc = new HttpClient())
                {
                    var rawJson = await wc.GetStringAsync(string.Format(ApiString, comicNum));
                    dynamic json = JsonConvert.DeserializeObject(rawJson);
                    string comicUri = json.img;
                    using (var stream = await wc.GetStreamAsync(comicUri))
                    {
                        await Program.Client.SendPhotoAsync(msg.Chat, stream.ToFileToSend("xkcd.png"),
                            (string) json.alt
                        );
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Silently fail
            }
        }
    }
}