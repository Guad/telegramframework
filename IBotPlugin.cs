using Telegram.Bot;

namespace CoreDumpedTelegramBot
{
    public interface IBotPlugin
    {
        void Hook(TelegramBotClient bot);
        void Update();
        void Start();
        void Stop();
    }
}