using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CoreDumpedTelegramBot
{

    public class CallbackHandler
    {
        class CallbackSettings
        {
            public Func<CallbackQuery, Task<bool>> Method;
            public bool SingleUse;
            public DateTime AttachTime;
            public DateTime? LastInvoke;
        }

        private Dictionary<string, CallbackSettings> _callbacks = new Dictionary<string, CallbackSettings>();

        public void AddCallback(string id, Func<CallbackQuery, Task<bool>> func)
        {
            CallbackSettings setts = new CallbackSettings();

            setts.Method = func;
            setts.SingleUse = false;
            setts.AttachTime = DateTime.UtcNow;

            lock (_callbacks)
            {
                if (!_callbacks.ContainsKey(id))
                    _callbacks.Add(id, setts);
                else _callbacks[id] = setts;
            }
        }

        public string AddCallback(Func<CallbackQuery, Task<bool>> func)
        {
            string id = Guid.NewGuid().ToString();

            CallbackSettings setts = new CallbackSettings();

            setts.Method = func;
            setts.SingleUse = false;
            setts.AttachTime = DateTime.UtcNow;

            lock (_callbacks)
            {
                if (!_callbacks.ContainsKey(id))
                    _callbacks.Add(id, setts);
                else _callbacks[id] = setts;
            }

            return id;
        }

        public string AddSingleCallback(Func<CallbackQuery, Task<bool>> func)
        {
            string id = Guid.NewGuid().ToString();

            CallbackSettings setts = new CallbackSettings();

            setts.Method = func;
            setts.SingleUse = true;
            setts.AttachTime = DateTime.UtcNow;

            lock (_callbacks)
            {
                if (!_callbacks.ContainsKey(id))
                    _callbacks.Add(id, setts);
                else _callbacks[id] = setts;
            }

            return id;
        }

        public void RemoveCallback(string id)
        {
            lock (_callbacks)
                _callbacks.Remove(id);
        }

        public void RemoveCallbacks(IEnumerable<string> callbacks)
        {
            lock (_callbacks)
            {
                foreach (string callback in callbacks)
                {
                    _callbacks.Remove(callback);
                }
            }
        }

        public async void Handle(CallbackQuery query)
        {
            CallbackSettings callback = null;
            lock (_callbacks)
                if (_callbacks.ContainsKey(query.Data))
                {
                    callback = _callbacks[query.Data];
                    callback.LastInvoke = DateTime.UtcNow;

                    if (callback.SingleUse)
                        _callbacks.Remove(query.Data);
                }

            if (callback != null)
            {
                try
                {
                    bool result = await callback.Method(query);
                    if (result && !callback.SingleUse)
                        lock (_callbacks)
                            _callbacks.Remove(query.Data);
                }
                catch (Exception ex)
                {
                    Program.HandleException(ex);
                }
            }
        }
    }
}