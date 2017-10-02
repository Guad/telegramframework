using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace CoreDumpedTelegramBot.Features
{
    public class Reminder : IBotPlugin
    {
        private List<RemindTimer> _reminders = new List<RemindTimer>();

        public void Hook(TelegramBotClient bot){}

        public void Start()
        {
            if (System.IO.File.Exists("reminders.json"))
            {
                lock (_reminders)
                    _reminders =
                        JsonConvert.DeserializeObject<List<RemindTimer>>(System.IO.File.ReadAllText("reminders.json"));
                System.IO.File.Delete("reminders.json");

                lock (_reminders)
                foreach (RemindTimer reminder in _reminders)
                {
                    if (reminder.ButtonCallback != null)
                        Program.CallbackHandler.AddCallback(reminder.ButtonCallback, async q =>
                        {
                            if (reminder.Users.Contains(q.From.Id))
                                await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Ya vas a ser recordado!");
                            else
                            {
                                reminder.Users.Add(q.From.Id);

                                await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Te lo recordaré!");
                            }

                            return false;
                        });
                }
            }
        }

        public void Stop()
        {
            if (_reminders.Count > 0)
            {
                System.IO.File.WriteAllText("reminders.json", JsonConvert.SerializeObject(_reminders));
            }
        }

        public async void Update()
        {
            List<RemindTimer> toAwait = new List<RemindTimer>();

            lock (_reminders)
            for (int i = _reminders.Count - 1; i >= 0; i--)
            {
                if (DateTime.UtcNow > _reminders[i].Trigger)
                {
                    toAwait.Add(_reminders[i]);
                    _reminders.RemoveAt(i);
                }
            }

            foreach (RemindTimer timer in toAwait)
            {
                await FireReminder(timer);
            }
        }

        public async Task FireReminder(RemindTimer remind)
        {
            if (remind.ButtonCallback != null)
                Program.CallbackHandler.RemoveCallback(remind.ButtonCallback);
            await Program.Client.DeleteMessageAsync(remind.OriginalChatId, remind.OriginalMessageId);

            foreach (int userId in remind.Users)
            {
                string msg = string.Format("¡Oye! Me dijiste que te recordara esto hace {0}:\n*{1}*", remind.RawTimeframe, remind.Text);
                await Program.Client.SendTextMessageAsync(userId, msg, ParseMode.Markdown);
            }
        }

        [Command(GreedyArg = true, Description = "Crear un recordatorio")]
        public async void remindme(Message msg, int cantidad, string timetype, string message)
        {
            if (cantidad <= 0)
                return;

            // Remove accents
            timetype = string.Concat(timetype.Normalize(NormalizationForm.FormD).Where(c =>
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
            string msgText = "Te enviaré un mensaje el *{0}* para recordarte esto:\n*{1}*";
            IReplyMarkup markup = null;

            RemindTimer newTimer = new RemindTimer();
            newTimer.Users = new List<int>();
            newTimer.Users.Add(msg.From.Id);
            newTimer.RawTimeframe = string.Format("{0} {1}", cantidad, timetype);
            newTimer.Text = message;
            
            object frame;
            if (!Enum.TryParse(typeof(TimeFrame), timetype, true, out frame))
                return;

            TimeFrame tframe = (TimeFrame) frame;

            TimeSpan span = GetTimeSpan(cantidad, tframe);
            newTimer.Trigger = DateTime.UtcNow.Add(span);

            if (msg.Chat.Type != ChatType.Private)
            {
                string callback = null;
                markup = new InlineKeyboardMarkup(new InlineKeyboardButton[1][]
                {
                    new InlineKeyboardButton[1]
                    {
                        new InlineKeyboardCallbackButton("¡A mí también!",
                        callback = Program.CallbackHandler.AddCallback(async q =>
                        {
                            if (newTimer.Users.Contains(q.From.Id))
                                await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Ya vas a ser recordado!");
                            else
                            {
                                newTimer.Users.Add(q.From.Id);

                                await Program.Client.AnswerCallbackQueryAsync(q.Id, "¡Te lo recordaré!");
                            }

                            return false;
                        }))
                    }
                });
                newTimer.ButtonCallback = callback;
            }

            Message origMsg = await Program.Client.SendTextMessageAsync(msg.Chat, string.Format(msgText, newTimer.Trigger.ToString("R"), message), ParseMode.Markdown,
                replyToMessageId: msg.MessageId, replyMarkup: markup);

            newTimer.OriginalChatId = origMsg.Chat.Id;
            newTimer.OriginalMessageId = origMsg.MessageId;

            lock (_reminders)
                _reminders.Add(newTimer);
        }

        private TimeSpan GetTimeSpan(int amount, TimeFrame frame)
        {
            TimeSpan span;
            switch ((int) frame)
            {
                case 0: // Seconds
                    span = TimeSpan.FromSeconds(amount);
                    break;
                case 1: // Minutes
                    span = TimeSpan.FromMinutes(amount);
                    break;
                case 2: // Hours
                    span = TimeSpan.FromHours(amount);
                    break;
                default:
                case 3: // Days
                    span = TimeSpan.FromDays(amount);
                    break;
                case 4: // Week
                    span = TimeSpan.FromDays(amount * 7);
                    break;
                case 5: // Months
                    span = TimeSpan.FromDays(amount * 30);
                    break;
                case 6: // Years
                    span = TimeSpan.FromDays(amount * 365);
                    break;
            }

            return span;
        }
    }

    public enum TimeFrame
    {
        Segundo = 0,
        Segundos = 0,
        S = 0,
        Seg = 0,
        Segs = 0,
        Sec = 0,
        Secs = 0,

        Minuto = 1,
        Minutos = 1,
        M = 1,
        Min = 1,
        Mins = 1,

        Hora = 2,
        Horas = 2,
        H = 2,

        Dia = 3,
        Dias = 3,
        D = 3,

        Semana = 4,
        Semanas = 4,

        Mes = 5,
        Meses = 5,

        Ano = 6, // jeje
        Anos = 7,
    }

    public class RemindTimer
    {
        public string RawTimeframe;
        public DateTime Trigger;
        public string Text;
        public List<int> Users;
        public string ButtonCallback;
        public int OriginalMessageId;
        public long OriginalChatId;
    }
}