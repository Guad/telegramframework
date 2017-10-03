using System;
using System.Collections.Generic;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot
{
    public class ChatData<T> where T : class
    {
        private Dictionary<long, T> _dict;

        public ChatData()
        {
            _dict = new Dictionary<long, T>();
        }

        public T this[long chatId]
        {
            get 
            {
                if (_dict.ContainsKey(chatId))
                    return _dict[chatId];
                T output;
                _dict.Add(chatId, output = (T)Activator.CreateInstance(typeof(T)));
                return output;
            }            
        }

        public T this[Chat chat]
        {
            get
            {
                return this[chat.Id];
            }
        }
    }
}