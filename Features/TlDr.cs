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
        private Dictionary<long, List<Message>> _messageBuffer = new Dictionary<long, List<Message>>();
        private Dictionary<long, DateTime> _lastMessages = new Dictionary<long, DateTime>();
        private const int BufferSize = 10;

        private static string ApiKey;

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
                List<Message> msgs;
                if (!_messageBuffer.ContainsKey(msg.Chat.Id))
                {
                    msgs = new List<Message>();
                    _messageBuffer.Add(msg.Chat.Id, msgs);
                }
                else msgs = _messageBuffer[msg.Chat.Id];

                msgs.Add(msg);
                if (msgs.Count > BufferSize)
                    msgs.RemoveAt(0);
            }
        }

        [Command(Description = "Leer un resumen del ultimo link enviado", Alias = "resumen")]
        public async void tldr(Message msg, int enunciados = 5)
        {            
            lock (_lastMessages)
            {
                if (_lastMessages.ContainsKey(msg.Chat.Id))
                {
                    if (DateTime.Now.Subtract(_lastMessages[msg.Chat.Id]).TotalSeconds < 10)
                        return;
                    else _lastMessages[msg.Chat.Id] = DateTime.Now;
                }
                else
                    _lastMessages.Add(msg.Chat.Id, DateTime.Now);
            }

            if (enunciados < 0)
                return;
            if (enunciados > 10)
                enunciados = 10;

            string uri = null;
            Message quoted = null;

            List<Message> buff = new List<Message>();
            if (_messageBuffer.ContainsKey(msg.Chat.Id))
                buff = _messageBuffer[msg.Chat.Id];

            lock (_messageBuffer)
                for (int i = _messageBuffer.Count - 1; i >= 0; i--)
                {
                    Match match = Regex.Match(buff[i].Text,
                        @"(http|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");
                    if (match.Success)
                    {
                        uri = match.Captures[0].Value;
                        quoted = buff[i];
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