#define IS_PLEER_BACK_UP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class MusicPlayer : IBotPlugin
    {
        private class RequestData
        {
            public User Requestee;
            public Message MusicMessage;
            public List<Song> Songs;
        }

        private ChatData<RequestData> Data = new ChatData<RequestData>();

        public void Hook(TelegramBotClient bot)
        {
            bot.OnMessage += BotOnOnMessage;
        }

        private async void BotOnOnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            var ourData = Data[messageEventArgs.Message.Chat];

            if (ourData.Requestee != null)
            {
                if (ourData.Requestee.Id != messageEventArgs.Message.From.Id) return;
                await Program.Client.DeleteMessageAsync(ourData.MusicMessage.Chat, ourData.MusicMessage.MessageId);
                int indx = messageEventArgs.Message.Text[0] - '0';
                if (indx >= 0 && indx < ourData.Songs.Count)
                    await PostSong(ourData.MusicMessage.Chat, ourData.Songs[indx]);
                Data.Remove(messageEventArgs.Message.Chat);
            }
        }

        public void Update(){}
        public void Start(){}
        public void Stop(){}

#if IS_PLEER_BACK_UP
        [Command(GreedyArg = true, Description = "Buscar una cancion")]
#endif
        public async void song(Message msg, string searchQuery)
        {
            var songs = await SearchSong(searchQuery);
            Message musicMessage = null;
            if (songs == null) return;
            var ourData = Data[msg.Chat];
            ourData.Songs = songs;
            ourData.Requestee = msg.From;

            List<KeyboardButton> buttons = new List<KeyboardButton>();

            string[] callbacks = new string[(int) Math.Min(10, songs.Count)];
            
            for (int i = 0; i < callbacks.Length; i++)
            {
                var i1 = i;
                var butten = new KeyboardButton(i + ". " + songs[i].Name + " - " + songs[i].Artist);
                buttons.Add(butten);
            }

            var markup = new ReplyKeyboardMarkup(buttons.Select(b => new KeyboardButton[1] { b }).ToArray(), oneTimeKeyboard: true);

            ourData.MusicMessage = await Program.Client.SendTextMessageAsync(msg.Chat, string.Format("Buscando resultados para *{0}*", searchQuery),
                ParseMode.Markdown, replyToMessageId: msg.MessageId, replyMarkup: markup);
        }

        private async Task PostSong(Chat c, Song s)
        {
            int tries = 0;
            again:
            try
            {
                using (var wc = new HttpClient())
                using (var stream = await wc.GetStreamAsync(s.Link))
                {
                    await Program.Client.SendAudioAsync(c,
                        stream.ToFileToSend(string.Format("{0} - {1}.mp3", s.Name, s.Artist)),
                        string.Format("Playing {0} by {1}", s.Name, s.Artist), 10, s.Artist, s.Name);
                }
            }
            catch (HttpRequestException)
            {
                if (tries++ > 3) return;
                await Task.Delay(2000);
                goto again;
            }
        }

        private async Task<List<Song>> SearchSong(string query)
        {
            int tries = 0;

            again:
            UriBuilder uriBuilder = new UriBuilder("http://mp3with.co/search");
            var parser = HttpUtility.ParseQueryString(string.Empty);
            parser["q"] = query;
            uriBuilder.Query = parser.ToString();
            string rawHtml;
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                try
                {
                    rawHtml = await client.DownloadStringTaskAsync(uriBuilder.Uri);
                }
                catch (WebException)
                {
                    //return null;
                    if (tries++ > 3)
                        return null;
                    await Task.Delay(2000);
                    goto again;
                }
            }
            List<Song> songs = new List<Song>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);
            //List<Song> output = new List<Song>();
            foreach (HtmlNode node in doc.DocumentNode.SelectSingleNode("//ul[@class=\"songs\"]").SelectNodes("li"))
            {
                songs.Add(new Song()
                {
                    Link = "http://mp3with.co" + node.Attributes["data-mp3"].Value,
                    Name = node.ChildNodes[1].ChildNodes
                        .First(n => n.Name == "strong" && !n.Attributes.Contains("class")).InnerHtml.Trim(),
                    Artist = node.ChildNodes[1].ChildNodes.First(n => n.GetAttributeValue("class", "none") == "artist")
                        .InnerHtml.Trim(),
                });
            }

            return songs;
        }

        private class Song
        {
            public string Name;
            public string Artist;
            public string Link;
        }
    }
    
}