using System;

namespace SAL.Core.Config
{
    internal class EventHandlerLstItem<TKey, TEventArgs>
    {
        public TKey Key;
        public EventHandler<TEventArgs> Handler;
    }
}