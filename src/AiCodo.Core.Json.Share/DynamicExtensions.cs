using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo
{
    public static class DynamicExtensions
    {
        public static DynamicEntity ToDynamicJson<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector, Func<TSource, object> elementSelector)
        {
            var d = new DynamicEntity();
            foreach (var g in source)
            {
                var key = keySelector(g);
                var value = elementSelector(g);
                d.SetValue(key, value);
            }
            return d;
        }

    }
}
