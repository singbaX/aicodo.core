// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    public static class CollectionExtensions
    {
        /// <summary>
        /// 递归取数据，深度优先
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="fnRecurse"></param>
        /// <returns></returns>
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> fnRecurse)
        {
            foreach (T item in source)
            {
                yield return item;
                IEnumerable<T> seqRecurse = fnRecurse(item);

                if (seqRecurse != null)
                {
                    foreach (T itemRecurse in Traverse(seqRecurse, fnRecurse))
                    {
                        yield return itemRecurse;
                    }
                }
            }
        }
        /// <summary>
        /// 转换成ObservableCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            ObservableCollection<T> list = new ObservableCollection<T>();
            foreach (T item in source)
                list.Add(item);
            return list;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source != null)
            {
                foreach (var item in source)
                {
                    action(item);
                }
            }
        }

        public static void ForEachWithFirst<T>(this IEnumerable<T> source, Action<T> firstAction, Action<T> action)
        {
            if (source != null)
            {
                bool first = true;
                foreach (var item in source)
                {
                    if (first)
                    {
                        firstAction(item);
                        first = false;
                    }
                    else
                    {
                        action(item);
                    }
                }
            }
        }

        public static void AddToCollection<T>(this IEnumerable<T> source, ICollection<T> target)
        {
            foreach (var item in source)
            {
                target.Add(item);
            }
        }


        public static Dictionary<string, object> ToDictionary(this object[] nameValues, bool keyToLower = false)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            if (nameValues == null || nameValues.Length == 0)
            {
                return dic;
            }
            for (int i = 0; i < nameValues.Length - 1; i += 2)
            {
                dic.Add(keyToLower ? nameValues[i].ToString().ToLower() : nameValues[i].ToString(), nameValues[i + 1]);
            }
            return dic;
        }

        public static object[] ToNameValues(this IDictionary<string, object> dic)
        {
            object[] nameValues = new object[dic.Count * 2];
            int i = 0;
            foreach (var item in dic)
            {
                nameValues[i] = item.Key;
                nameValues[i + 1] = item.Value;
                i = i + 2;
            }

            return nameValues;
        }

        public static Dictionary<string, string> ToDictionary(this string keyValues, char itemSplit = '|', char split = ':')
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            if (keyValues.IsNullOrEmpty())
            {
                return dic;
            }
            foreach (var item in keyValues.Split(itemSplit))
            {
                string[] kv = item.Split(split);
                if (kv.Length > 1)
                {
                    dic.Add(kv[0], kv[1]);
                }
            }
            return dic;
        }

        public static TValue GetStringDictionaryValue<TValue>(this Dictionary<string, TValue> dic, string key, TValue defaultValue)
        {
            foreach (var item in dic)
            {
                if (item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }
            return defaultValue;
        }

        public static TValue GetDictionaryValue<Tkey, TValue>(this Dictionary<Tkey, TValue> dic,
            Tkey key, TValue defaultValue)
        {
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }
            return defaultValue;
        }

        public static string AggregateToString<T>(this IEnumerable<T> sources, Func<T, string> format = null, string split = ",")
        {
            if (sources == null || sources.Count() == 0)
            {
                return "";
            }

            if (format == null)
            {
                format = t => t.ToString();
            }
            if (sources.Count() == 1)
            {
                var item = sources.First();
                return format(item);
            }

            StringBuilder sb = new StringBuilder();
            sources.ForEachWithFirst(
                (s) => { sb.Append(format(s)); },
                (s) => { sb.Append(split); sb.Append(format(s)); });
            return sb.ToString();
        }

        /// <summary>
        /// 组合成字符串
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="split">分隔符，默认为‘，’</param>
        /// <returns></returns>
        public static string AggregateStrings(this IEnumerable<string> sources, string split = ",")
        {
            if (sources == null || sources.Count() == 0)
            {
                return "";
            }
            if (sources.Count() == 1)
            {
                return sources.First();
            }
            StringBuilder sb = new StringBuilder();
            sources.ForEachWithFirst(
                (s) => { sb.Append(s); },
                (s) => { sb.Append(split); sb.Append(s); });
            return sb.ToString();
        }

        /// <summary>
        /// 集合组合成字符串，各个元素都可以有多个分隔字符，最终集合会去掉重复值
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="split"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static string AggregateSplitStrings(this IEnumerable<string> sources, string split = ",", bool ignoreCase = true)
        {
            if (sources == null || sources.Count() == 0)
            {
                return "";
            }

            List<string> targets = new List<string>();
            Action<string> addItem = (str) =>
            {
                if (ignoreCase ? targets.FirstOrDefault(t => t.Equals(str, StringComparison.OrdinalIgnoreCase)) == null :
                    targets.FirstOrDefault(t => t.Equals(str)) == null)
                {
                    targets.Add(str);
                }
            };

            foreach (var item in sources)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                var items = item.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in items)
                {
                    addItem(s);
                }
            }
            if (targets.Count == 0)
            {
                return "";
            }
            else if (targets.Count == 1)
            {
                return targets[0];
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                targets.ForEachWithFirst(
                    (s) => { sb.Append(s); },
                    (s) => { sb.Append(split); sb.Append(s); });
                return sb.ToString();
            }
        }

        public static string MergeAggregateSplitStrings(this string source, string split = ",", params string[] addItems)
        {
            var list = source.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries).ToList();
            addItems.ForEach(item =>
            {
                item.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !list.Contains(s))
                    .AddToCollection(list);
            });

            var sb = new StringBuilder();
            foreach (var s in list)
            {
                if (sb.Length > 0)
                {
                    sb.Append(split);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }
        public static string RemoveAggregateSplitStrings(this string source, string split = ",", params string[] addItems)
        {
            var list = source.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries).ToList();
            addItems.ForEach(item =>
            {
                item.Split(new string[] { split }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => list.Contains(s))
                    .ForEach(s => list.Remove(s));
            });

            var sb = new StringBuilder();
            foreach (var s in list)
            {
                if (sb.Length > 0)
                {
                    sb.Append(split);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }

        public static IEnumerable<T> Merge<T>(this IEnumerable<T> items1, IEnumerable<T> items2)
        {
            foreach (var item in items1)
            {
                yield return item;
            }

            foreach (var item in items2)
            {
                yield return item;
            }
        }

        public static IEnumerable<T> Merge<T>(this IEnumerable<T> items1, IEnumerable<T> items2, Func<T, T, bool> isEquals)
        {
            List<T> items = new List<T>();
            foreach (var item in items1)
            {
                if (items.FirstOrDefault(f => isEquals(f, item)) == null)
                {
                    items.Add(item);
                }
            }

            foreach (var item in items2)
            {
                if (items.FirstOrDefault(f => isEquals(f, item)) == null)
                {
                    items.Add(item);
                }
            }
            return items;
        }

        public static void ResetKeys<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TKey> formatkey)
        {
            dic.ToList().ForEach(d =>
            {
                dic.Remove(d.Key);
                dic.Add(formatkey(d.Key), d.Value);
            });
        }
    }
}
