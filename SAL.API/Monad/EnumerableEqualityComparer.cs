using System;
using System.Collections.Generic;

namespace SAL.API.Monad
{
    public static class MonadExtension
    {
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
        {
            foreach (var src in source)
            {
                action(src);
            }

        }

        public static void TryForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
        {
            foreach (var src in source)
            {
                try
                {
                    action(src);
                }
                catch (System.Exception)
                {
                    //
                }

            }

        }
    }


}
