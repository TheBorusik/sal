using System;
using System.Collections.Generic;

namespace SAL.Core.Configuration
{
    internal class EventHandlerList<TKey, TEventArgs>
    {
        private readonly LinkedList<EventHandlerLstItem<TKey, TEventArgs>> handlerList = new LinkedList<EventHandlerLstItem<TKey, TEventArgs>>();
        private object locker = new object();


        public EventHandler<TEventArgs> this[TKey key] => Find(key)?.Handler;


        public void AddHandler(TKey key, EventHandler<TEventArgs> value)
        {
            var e = Find(key);
            if (e != null)
            {
                lock (locker)
                    e.Handler += value;
            }
            else
            {
                lock (locker)
                    handlerList.AddLast(new EventHandlerLstItem<TKey, TEventArgs>
                    {
                        Key = key,
                        Handler = value
                    });
            }
        }

        public void RemoveHandler(TKey key, EventHandler<TEventArgs> value)
        {
            var e = Find(key);
            if (e != null)
            {
                if (value != null)
                    e.Handler -= value;
            }

        }

        private EventHandlerLstItem<TKey, TEventArgs> Find(TKey key)
        {
            var found = handlerList.First;
            while (found != null)
            {
                if (Equals(found.Value.Key,key))   
                    break;
                
                found = found.Next;
            }

            return found?.Value;
        }
    }
}