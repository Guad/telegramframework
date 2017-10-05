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
                lock (_dict)
                {
                    if (_dict.ContainsKey(chatId))
                        return _dict[chatId];
                    T output;
                    lock (_dict)
                        _dict.Add(chatId, output = (T) Activator.CreateInstance(typeof(T)));
                    return output;
                }
            }
            set
            {
                lock (_dict)
                {
                    if (_dict.ContainsKey(chatId))
                        _dict[chatId] = value;
                    else _dict.Add(chatId, value);
                }
            }
        }

        public T this[Chat chat]
        {
            get => this[chat.Id];
            set => this[chat.Id] = value;
        }
    }
}