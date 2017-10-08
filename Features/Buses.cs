using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class Buses : IBotPlugin
    {
        private class SavedStops
        {
            public Dictionary<int, string> Stops = new Dictionary<int, string>();
            public User Requester;
            public Message RequestMessage;
        }

        private ChatData<SavedStops> _savedBuses;

        private async Task<string> GetStopText(int stop, string desc = null)
        {
            var buses = await Endpoints.GetEstimatesIncident(stop);

            if (buses.arrives == null)
                return "No hay buses o esa parada no existe!";

            if (desc == null)
                desc = stop.ToString();

            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format("Proximos buses para *{0}*:\n", desc));

            foreach (var bus in buses.arrives)
            {
                var span = TimeSpan.FromSeconds(bus.busTimeLeft);
                sb.Append(string.Format("Linea *{0}* - {1}' (dist: {2}m)\n", bus.lineId,
                    span.Minutes > 20 ? "+20" : span.Minutes.ToString(), bus.busDistance));
            }

            return sb.ToString();
        }

        [Command(Description = "Proximos buses de la parada en Madrid")]
        public async void bus(Message msg, int idParada)
        {
            var txt = await GetStopText(idParada);
            
            await Program.Client.SendTextMessageAsync(msg.Chat, txt, ParseMode.Markdown, replyToMessageId: msg.MessageId);
        }

        [Command(Description = "Guardar una parada como favorito", GreedyArg = true)]
        public async void savebus(Message msg, int idParada, string descripcion)
        {
            var ourData = _savedBuses[msg.Chat];

            if (ourData.Stops.ContainsKey(idParada))
            {
                ourData.Stops[idParada] = descripcion;
                await Program.Client.SendTextMessageAsync(msg.Chat,
                    "Ya tengo esa parada guardada. He actualizado su descripción.", replyToMessageId: msg.MessageId);
            }
            else
            {
                ourData.Stops.Add(idParada, descripcion);
                await Program.Client.SendTextMessageAsync(msg.Chat,
                    "He guardado tu parada! Para ver todas las paradas utiliza /" + nameof(paradas));
            }
        }

        [Command(Description = "Eliminar una parada favorita")]
        public void delbus(Message msg, int idParada)
        {
            var ourData = _savedBuses[msg.Chat];

            ourData.Stops.Remove(idParada);
        }

        [Command(Description = "Selecciona una parada de una la lista de favoritos")]
        public async void paradas(Message msg)
        {
            var ourData = _savedBuses[msg.Chat];

            if (ourData.Stops.Count == 0)
            {
                await Program.Client.SendTextMessageAsync(msg.Chat, "No hay ninguna parada favorita!",
                    replyToMessageId: msg.MessageId);
            }
            else
            {
                if (ourData.RequestMessage != null)
                {
                    await Program.Client.DeleteMessageAsync(ourData.RequestMessage.Chat,
                        ourData.RequestMessage.MessageId);
                    ourData.RequestMessage = null;
                }

                ourData.Requester = msg.From;

                List<KeyboardButton> buttons = new List<KeyboardButton>();

                foreach (var pair in ourData.Stops)
                {
                    var butten = new KeyboardButton(pair.Key + ". " + pair.Value);
                    buttons.Add(butten);
                }

                var markup = new ReplyKeyboardMarkup(buttons.Select(b => new KeyboardButton[1] { b }).ToArray(), oneTimeKeyboard: true);

                ourData.RequestMessage = await Program.Client.SendTextMessageAsync(msg.Chat, "Elige una parada",
                    replyToMessageId: msg.MessageId, replyMarkup: markup);
            }
            
        }

        public void Hook(TelegramBotClient bot)
        {
            bot.OnMessage += Bot_OnMessage;
        }

        private async void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var msg = e.Message;
            if (msg.Type != MessageType.TextMessage) return;
            var data = _savedBuses[msg.Chat];

            if (data.Requester != null && data.Requester.Id == msg.From.Id && msg.Text.Contains('.'))
            {
                int indx = msg.Text.IndexOf('.');

                await Program.Client.DeleteMessageAsync(data.RequestMessage.Chat,
                    data.RequestMessage.MessageId);
                data.RequestMessage = null;
                data.Requester = null;

                int parada;
                if (!int.TryParse(msg.Text.Substring(0, indx), out parada))
                    return;

                string paradaName = null;

                if (msg.Text.Length > indx + 2)
                    paradaName = msg.Text.Substring(indx + 1).Trim();

                await Program.Client.SendTextMessageAsync(msg.Chat, await GetStopText(parada, paradaName), ParseMode.Markdown);
            }
        }

        public void Update()
        {
        }

        public void Start()
        {
            if (System.IO.File.Exists("paradas.json"))
            {
                var raw = JsonConvert.DeserializeObject<Dictionary<long, SavedStops>>(
                    System.IO.File.ReadAllText("paradas.json"));
                _savedBuses = new ChatData<SavedStops>(raw);
            }
            else
            {
                _savedBuses = new ChatData<SavedStops>();
            }
        }

        public void Stop()
        {
            Dictionary<long, SavedStops> paradasRaw =
                (Dictionary<long, SavedStops>) _savedBuses;

            System.IO.File.WriteAllText("paradas.json", JsonConvert.SerializeObject(paradasRaw));
        }

        private static class Endpoints
        {
            public static async Task<ApiResponse> GetEstimatesIncident(int stopId)
            {
                var request = new ApiRequest("/media/GetEstimatesIncident.php");

                request.Data.Add("idStop", stopId.ToString());

                request.Data.Add("Text_StopRequired_YN", "N");
                request.Data.Add("Audio_StopRequired_YN", "N");
                request.Data.Add("Text_EstimationsRequired_YN", "Y");
                request.Data.Add("Audio_EstimationsRequired_YN", "N");
                request.Data.Add("Text_IncidencesRequired_YN", "N");
                request.Data.Add("Audio_IncidencesRequired_YN", "N");
                request.Data.Add("cultureInfo", "EN");

                return await request.SendAsync();
            }
        }

        private class BusArrive
        {
            public string lineId;
            public string destination;
            public int busId;
            public int busTimeLeft;
            public int busDistance;
            public double latitude;
            public double longitude;
        }

        private class ApiResponse
        {
            public int errorCode { get; set; }
            public string description { get; set; }
            public List<BusArrive> arrives { get; set; }
        }

        private class ApiRequest
        {
            private const string _uri = @"https://openbus.emtmadrid.es:9443/emt-proxy-server/last";

            public string IdClient;
            public string Passkey;
            public string Endpoint;

            public Dictionary<string, string> Data;

            public ApiRequest(string endpoint)
            {
                IdClient = "EMT.SERVICIOS.IPHONE.2.0";
                Passkey = "DC352ADB-F31D-41E5-9B95-CCE11B7921F4"; // Public key
                Endpoint = endpoint;
                Data = new Dictionary<string, string>();
            }

            public async Task<ApiResponse> SendAsync()
            {
                JObject json;
                HttpResponseMessage response;
                ApiResponse apiResp;

                Data.Add("idClient", IdClient);
                Data.Add("passKey", Passkey);

                HttpContent content = new FormUrlEncodedContent(Data.ToArray());

                using (HttpClient client = new HttpClient())
                {
                    response = await client.PostAsync(_uri + Endpoint, content);
                }

                using (var stream = new MemoryStream())
                {
                    await response.Content.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream))
                    using (JsonTextReader jsonReader = new JsonTextReader(reader))
                    {
                        json = JObject.Load(jsonReader);
                    }
                }

                apiResp = new ApiResponse();
                apiResp.errorCode = json["errorCode"].Value<int>();
                apiResp.description = json["description"].Value<string>();

                if (apiResp.errorCode == 0)
                {
                    JObject singleArrive = json["arrives"]["arriveEstimationList"]["arrive"] as JObject;
                    JArray arrives = json["arrives"]["arriveEstimationList"]["arrive"] as JArray;
                    apiResp.arrives = new List<BusArrive>();

                    if (singleArrive != null)
                    {
                        BusArrive barr = new BusArrive();
                        barr.busId = singleArrive["busId"].Value<int>();
                        barr.lineId = singleArrive["lineId"].Value<string>();
                        barr.busDistance = singleArrive["busDistance"].Value<int>();
                        barr.busTimeLeft = singleArrive["busTimeLeft"].Value<int>();
                        barr.destination = singleArrive["destination"].Value<string>();
                        barr.longitude = singleArrive["longitude"].Value<double>();
                        barr.latitude = singleArrive["latitude"].Value<double>();

                        apiResp.arrives.Add(barr);
                    }
                    else foreach (JToken arrive in arrives)
                        {
                            BusArrive barr = new BusArrive();
                            barr.busId = arrive["busId"].Value<int>();
                            barr.lineId = arrive["lineId"].Value<string>();
                            barr.busDistance = arrive["busDistance"].Value<int>();
                            barr.busTimeLeft = arrive["busTimeLeft"].Value<int>();
                            barr.destination = arrive["destination"].Value<string>();
                            barr.longitude = arrive["longitude"].Value<double>();
                            barr.latitude = arrive["latitude"].Value<double>();

                            apiResp.arrives.Add(barr);
                        }
                }

                return apiResp;

            }
        }
    }
}