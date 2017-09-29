using System;
using System.IO;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace CoreDumpedTelegramBot
{
    public static class Util
    {
        public static FileToSend ToTelegramFile(this Stream stream, string filename)
        {
            return new FileToSend(filename, stream);
        }
    }
}