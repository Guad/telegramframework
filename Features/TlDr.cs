using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CoreDumpedTelegramBot.Features
{
    public class TlDr : IBotPlugin
    {
        private List<Message> _messageBuffer = new List<Message>();
        private const int BufferSize = 10;

        private static string ApiKey;
        private DateTime _lastMessage = DateTime.Now;

        public void Hook(TelegramBotClient bot)
        {
            bot.OnMessage += Bot_OnMessage;
        }

        private void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;

            if (msg == null || msg.Type != MessageType.TextMessage)
                return;

            lock (_messageBuffer)
            {
                _messageBuffer.Add(msg);
                if (_messageBuffer.Count > BufferSize)
                    _messageBuffer.RemoveAt(0);
            }
        }

        [Command(Description = "Leer un resumen del ultimo link enviado", Alias = "resumen")]
        public async void tldr(Message msg, int enunciados = 5)
        {
            if (DateTime.Now.Subtract(_lastMessage).TotalSeconds < 10)
                return; // Prevent spam

            if (enunciados < 0)
                return;
            if (enunciados > 10)
                enunciados = 10;

            string uri = null;
            Message quoted = null;

            lock (_messageBuffer)
                for (int i = _messageBuffer.Count - 1; i >= 0; i--)
                {
                    Match match = Regex.Match(_messageBuffer[i].Text,
                        @"(http|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");
                    if (match.Success)
                    {
                        uri = match.Captures[0].Value;
                        quoted = _messageBuffer[i];
                        break;
                    }
                }

            if (string.IsNullOrWhiteSpace(uri))
                return;

            var summary = await GetSummary(uri, enunciados);

            string reply = string.Format("*{0}*\n{1}", summary.Title, summary.Text);

            await Program.Client.SendTextMessageAsync(msg.Chat, reply, ParseMode.Markdown,
                replyToMessageId: quoted.MessageId);
        }

        private async Task<Summary> GetSummary(string url, int sentences = 7)
        {
            using (var wc = new HttpClient())
            {
                var rawjson = await wc.GetStringAsync(
                    string.Format("http://api.smmry.com/?SM_API_KEY={0}&SM_LENGTH={1}&SM_WITH_BREAK=1&SM_URL={2}", ApiKey, sentences, url));

                dynamic json = JsonConvert.DeserializeObject(rawjson);

                Summary sum = new Summary();

                sum.Title = json.sm_api_title;
                sum.Text = json.sm_api_content;

                sum.Title = sum.Title.Replace("[BREAK]", "\n");
                sum.Text = sum.Text.Replace("[BREAK]", "\n");

                return sum;
            }
        }

        public void Update()
        {
        }

        public void Start()
        {
            ApiKey = Environment.GetEnvironmentVariable("SMMRY_API_KEY");
            if (string.IsNullOrWhiteSpace(ApiKey))
                Program.ConsoleLog("You have forgot to set the SMMRY_API_KEY!");
        }

        public void Stop()
        {
        }

        private class Summary
        {
            public string Text;
            public string Title;
        }
    }
}