using System;

namespace SAL.API
{
    public static class EventHelper
    {
        public static bool CheckIsMyEvent(this EventContext context)
        {
            return context.Descriptor.SourceAdapterName == AdapterConfiguration.AdapterName &&
                   context.Descriptor.SourceAdapterType == AdapterConfiguration.AdapterType;
        }

        public static bool CheckIsExpire(this EventContext context, TimeSpan ttl)
        {
            return context.Descriptor.PublishTimeStamp + ttl <= DateTime.UtcNow;
        }
    }
}
