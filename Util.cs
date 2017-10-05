using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace CoreDumpedTelegramBot
{
    public static class Util
    {
        public static DateTime ToMadridTime(this DateTime time)
        {
            // Madrid is GMT+2
            return TimeZoneInfo.ConvertTimeToUtc(time).AddHours(2);
        }

        public static string ToHumanString(this DateTime time)
        {
            return time.ToString("d MMM yyyy HH:mm:ss");
        }

        public static async Task DeleteAfter(this Message msg, int seconds = 10)
        {
            await Task.Delay(seconds);

            await Program.Client.DeleteMessageAsync(msg.Chat, msg.MessageId);
        }
    }
}