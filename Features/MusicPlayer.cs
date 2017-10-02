//#define IS_PLEER_BACK_UP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class MusicPlayer : IBotPlugin
    {
        public void Hook(TelegramBotClient bot){}
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
            List<KeyboardButton> buttons = new List<KeyboardButton>();
            string[] callbacks = new string[(int) Math.Min(3, songs.Count)];

            for (int i = 0; i < callbacks.Length; i++)
            {
                var i1 = i;
                var butten = new InlineKeyboardCallbackButton(songs[i].Name + " - " + songs[i].Artist,
                    callbacks[i] = Program.CallbackHandler.AddCallback(async cback =>
                    {
                        if (cback.From.Id != msg.From.Id) return false;
                        Program.CallbackHandler.RemoveCallbacks(callbacks);
                        await Program.Client.DeleteMessageAsync(msg.Chat, musicMessage.MessageId);

                        await PostSong(msg.Chat, songs[i1]);
                        return true;
                    }));
                buttons.Add(butten);
            }

            var markup = new ReplyKeyboardMarkup(buttons.Select(b => new KeyboardButton[1] { b  }).ToArray());

            musicMessage = await Program.Client.SendTextMessageAsync(msg.Chat, string.Format("Buscando resultados: *{0}*:", searchQuery),
                ParseMode.Markdown, replyToMessageId: msg.MessageId, replyMarkup: markup);
        }

        private async Task PostSong(Chat c, Song s)
        {
            using (var wc = new HttpClient())
            using (var stream = await wc.GetStreamAsync(s.Link))
            {
                await Program.Client.SendAudioAsync(c,
                    stream.ToFileToSend(string.Format("{0} - {1}.mp3", s.Name, s.Artist)),
                    string.Format("Playing {0} by {1}", s.Name, s.Artist), 100, s.Artist, s.Name);
            }
        }

        private async Task<List<Song>> SearchSong(string query)
        {
            string uri = @"http://pleer.net/search?q=" + HttpUtility.UrlEncode(query);

            List<Song> songs = new List<Song>();

            using (var wc = new HttpClient())
            {
                var html = await wc.GetStringAsync(uri);
                html = html.Replace("\n", " ");

                var matches = Regex.Matches(html, "<li duration(.+?)>");

                foreach (var match in matches)
                {
                    string line = ((Match) match).Value;

                    Song s = new Song();

                    foreach (var pair in Regex.Matches(line, "\\s(.+?)=\"(.*?)\""))
                    {
                        var m = (Match) pair;
                        if (string.IsNullOrWhiteSpace(m.Value)) continue;
                        string value = m.Groups[2].Captures[0].ToString().Trim();
                        switch (m.Groups[1].Captures[0].ToString().Trim())
                        {
                            case "link":
                                string uri_json =
                                    @"http://pleer.net/site_api/files/get_url?action=download&id=" +
                                    value;
                                var json = await wc.GetStringAsync(uri_json);

                                var r = Regex.Match(json, "\"track_link\":\"(.+?)\"");
                                s.Link = r.Groups[1].Captures[0].ToString();
                                break;
                            case "singer":
                                s.Artist = HttpUtility.HtmlDecode(value);
                                break;
                            case "song":
                                s.Name = HttpUtility.HtmlDecode(value);
                                break;
                            /*case "size":
                                s.Source = source;
                                break;*/
                        }

                    }

                    songs.Add(s);
                }
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