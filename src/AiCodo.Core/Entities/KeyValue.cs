// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AiCodo
{
    public class KeyValueItem<T> : EntityBase
    {
        #region 属性 Key
        private string _Key = string.Empty;
        public string Key
        {
            get
            {
                return _Key;
            }
            set
            {
                _Key = value;
                RaisePropertyChanged("Key");
            }
        }
        #endregion

        #region 属性 Value
        private T _Value = default(T);
        public T Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
                RaisePropertyChanged("Value");
            }
        }
        #endregion

        public KeyValueItem()
        {

        }

        public KeyValueItem(string key, T value)
        {
            Key = key;
            Value = value;
        }
    }

    public class KeyValueItem : KeyValueItem<string>
    {
    }

    public class KeyValueCollection : CollectionBase<KeyValueItem>
    {
        #region 属性 StringValues

        private string _StringValues = "";

        public string StringValues
        {
            get { return _StringValues; }
            set
            {
                _StringValues = value;
                ResetItems();
            }
        }

        #endregion

        public KeyValueItem this[string key]
        {
            get
            {
                return GetItem(key);
            }
        }

        private KeyValueItem GetItem(string key, string defaultValue = "")
        {
            var item = this.FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = new KeyValueItem { Key = key, Value = defaultValue };
                this.Add(item);
            }
            return item;
        }

        public string GetValue(string key, string defaultValue = "")
        {
            var item = GetItem(key, defaultValue);
            return item.Value;
        }

        private void ResetItems()
        {
            this.Clear();
            foreach (KeyValueItem item in StringValues.ToKeyValues('|', ':'))
            {
                this.Add(item);
            }
        }

        
    }
    public static class KeyValueHelper
    {
        public static IEnumerable KeyValueDataSource(string[] keyValues)
        {
            if (keyValues == null || keyValues.Length == 0)
            {
                yield break;
            }
            foreach (string item in keyValues)
            {
                string[] kv = item.Split(':');
                var kitem = new KeyValueItem();
                if (kv.Length > 0)
                {
                    kitem.Key = kv[0];
                }
                if (kv.Length > 1)
                {
                    kitem.Value = kv[1];
                }
                yield return kitem;
            }
        }

        public static IEnumerable<KeyValueItem> ToKeyValues(this string keyValues, char itemSplit = '|', char split = ':')
        {
            if (string.IsNullOrEmpty(keyValues))
                yield break;
            foreach (string item in keyValues.Split(itemSplit))
            {
                string[] kv = item.Split(split);
                var kitem = new KeyValueItem();
                if (kv.Length > 0)
                {
                    kitem.Key = kv[0];
                }
                if (kv.Length > 1)
                {
                    kitem.Value = kv[1];
                }
                yield return kitem;
            }
        }

        public static string ToString(this IEnumerable<KeyValueItem> keyValues, char itemSplit = '|', char split = ':')
        {
            var sb = new StringBuilder();
            foreach (KeyValueItem item in keyValues)
            {
                sb.AppendFormat("{0}{1}{2}{3}", item.Key, split, item.Value, itemSplit);
            }
            return sb.ToString();
        }
    }
}

