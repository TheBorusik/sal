using System;

namespace SAL.Core.Configuration
{
    internal class EventHandlerLstItem<TKey, TEventArgs>
    {
        public TKey Key;
        public EventHandler<TEventArgs> Handler;
    }
}