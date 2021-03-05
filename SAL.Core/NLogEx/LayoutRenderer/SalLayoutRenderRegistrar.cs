using System;
using System.Collections.Concurrent;
using SAL.API;

namespace SAL.Core.NLogEx.LayoutRenderer
{

    public static class SalLayoutRenderRegistrar
    {
        private static ConcurrentDictionary<string, Type> layoutRenderers = new ConcurrentDictionary<string, Type>();

        public static void AddLayoutRenderer(string name, Type layoutRenderer)
        {
            layoutRenderers.AddOrUpdate(name, layoutRenderer, (n, t) => layoutRenderer);
        }

        internal static void Register(Action<string, Type> register)
        {
            layoutRenderers.ForEach(kv =>
            {
                register(kv.Key, kv.Value);
            });
        }
    }


}