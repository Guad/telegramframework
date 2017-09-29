using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace CoreDumpedTelegramBot
{
    class Program
    {
        private static string ApiKey;
        private static int[] Developers;

        private static List<IBotPlugin> _plugins;
        private static bool _stop;
        private static CommandHandler _cmdHandler;

        public static CallbackHandler CallbackHandler;
        public static TelegramBotClient Client;

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Starting...");
            ApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

            Console.WriteLine("Got envvar TELEGRAM_API_KEY: " + ApiKey);

            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Console.WriteLine("You are missing the TELEGRAM_API_KEY environment variable!");
                return;
            }

            string devs = Environment.GetEnvironmentVariable("TELEGRAM_DEVELOPERS");

            Console.WriteLine("Got envvar TELEGRAM_DEVELOPERS: " + devs);

            if (!string.IsNullOrWhiteSpace(devs))
                Developers = devs.Split(',').Select(int.Parse).ToArray();
            else Developers = new int[0];
            
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _stop = true;
                eventArgs.Cancel = true;
            };

            Console.WriteLine("Initializing...");

            Client = new TelegramBotClient(ApiKey);
            _plugins = new List<IBotPlugin>();
            _cmdHandler = new CommandHandler();
            CallbackHandler = new CallbackHandler();

            Console.WriteLine("Looking for plugins...");

            Assembly ass = Assembly.GetEntryAssembly();

            foreach (TypeInfo ti in ass.DefinedTypes)
                if (ti.ImplementedInterfaces.Contains(typeof(IBotPlugin)))
                    _plugins.Add(ass.CreateInstance(ti.FullName) as IBotPlugin);

            Console.WriteLine("Starting command handler...");

            _cmdHandler = new CommandHandler();
            _cmdHandler.Initialize(_plugins);

            Client.OnMessage += ClientOnOnMessage;
            Client.OnCallbackQuery += ClientOnOnCallbackQuery;

            Console.WriteLine("Hooking plugins...");

            _plugins.ForEach(TryWithException<IBotPlugin>(p => p.Hook(Client)));

            Console.WriteLine("Starting receiving...");

            Client.StartReceiving();

            Console.WriteLine("Starting plugins...");

            _plugins.ForEach(TryWithException<IBotPlugin>(p => p.Start()));

            Console.WriteLine("Starting main loop...");

            while (!_stop)
            {
                _plugins.ForEach(TryWithException<IBotPlugin>(p => p.Update()));
                await Task.Delay(500);
            }

            _plugins.ForEach(TryWithException<IBotPlugin>(p => p.Stop()));

            Console.WriteLine("Exiting...");

            Client.StopReceiving();
        }

        private static void ClientOnOnCallbackQuery(object o, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            CallbackHandler.Handle(callbackQueryEventArgs.CallbackQuery);
        }

        private static void ClientOnOnMessage(object o, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.Message != null && messageEventArgs.Message.Type == MessageType.TextMessage &&
                !string.IsNullOrWhiteSpace(messageEventArgs.Message.Text) &&
                messageEventArgs.Message.Text.TrimStart().StartsWith("/"))
            {
                _cmdHandler.Handle(messageEventArgs.Message);
            }
        }

        public static void ConsoleLog(string text,
            [System.Runtime.CompilerServices.CallerFilePath] string callingClass = null)
        {
            if (!string.IsNullOrEmpty(callingClass))
                callingClass = Path.GetFileNameWithoutExtension(callingClass);

            Console.WriteLine("[{0}] {1}: {2}", DateTime.UtcNow.ToString("(dd)HH:mm:ss.fff"), callingClass, text);
        }

        public static async void SendToDevelopers(string text,
            [System.Runtime.CompilerServices.CallerFilePath] string callingClass = null)
        {
            if (!string.IsNullOrEmpty(callingClass))
                callingClass = Path.GetFileNameWithoutExtension(callingClass);

            for (int i = 0; i < Developers.Length; i++)
            {
                await Client.SendTextMessageAsync(Developers[i], string.Format("[{0}] {1}", callingClass, text));
            }
        }
        
        public static void HandleException(Exception ex)
        {
            string msg = "UNHANDLED EXCEPTION AT " + DateTime.UtcNow.ToString("(dd)HH:mm:ss.fff") +
                         Environment.NewLine + ex.ToString();
            ConsoleLog(msg);
            SendToDevelopers(msg);
        }
        public static Action<T> TryWithException<T>(Action<T> a)
        {
            return new Action<T>(val =>
            {
                try
                {
                    a(val);
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
            });
        }
    }
}
