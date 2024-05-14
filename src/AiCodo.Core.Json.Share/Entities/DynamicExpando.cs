// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
#if Newton
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
#else
    using System.Text.Json.Serialization;
#endif
    using System.Collections.ObjectModel;

    /// <summary>
    /// 动态类
    /// </summary>
#if Newton
    [JsonConverter(typeof(DynamicExpandoJsonConverter<DynamicExpando>))]
#endif
    public class DynamicExpando : System.Dynamic.DynamicObject, IDictionary<string, object>, IEntity
    {
        #region 实现动态操作
        IDictionary<string, object> data = new Dictionary<string, object>();

        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
        {
            SetValue(binder.Name, value);
            return true;
        }

        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            result = GetValue(binder.Name, null);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return data.Keys;
        }

        public virtual IEnumerable<string> GetFieldNames()
        {
            return data.Keys;
        }

        public virtual object[] GetNameValues()
        {
            return data.ToNameValues();
        }

        /// <summary>
        /// 这个命令不会触发PropertyChanged事件
        /// </summary>
        /// <param name="nameValues"></param>
        public virtual void SetNameValues(params object[] nameValues)
        {
            if (nameValues == null || nameValues.Length == 0)
            {
                return;
            }
            for (int i = 0; i < nameValues.Length - 1; i += 2)
            {
                //SetValue(nameValues[i].ToString(), nameValues[i + 1]);
                data[nameValues[i].ToString()] = nameValues[i + 1];
            }
        }

        public virtual void SetValue(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            data[key] = value is DBNull ? null : value;
            RaisePropertyChanged(key);
        }

        public virtual object GetValue(string key, object defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (data.TryGetValue(key, out object dataValue))
            {
            }

            if (dataValue == null && defaultValue != null)
            {
                data[key] = defaultValue;
                dataValue = defaultValue;
                RaisePropertyChanged(key);
            }

            return dataValue;
        }

        public virtual void RemoveKey(string key)
        {
            if (data.ContainsKey(key))
            {
                data.Remove(key);
            }
        }

        public IReadOnlyDictionary<string, object> GetData()
        {
            return new ReadOnlyDictionary<string, object>(data);
        }
        #endregion

        #region IDictionary<string, object> members
        public void Add(string key, object value)
        {
            SetValue(key, value);
        }

        public bool ContainsKey(string key)
        {
            return data.ContainsKey(key);
        }

        [System.Xml.Serialization.XmlIgnore, JsonIgnore]
        public ICollection<string> Keys
        {
            get
            {
                return data.Keys;
            }
        }

        [System.Xml.Serialization.XmlIgnore, JsonIgnore]
        public ICollection<object> Values
        {
            get
            {
                return data.Values;
            }
        }

        public bool Remove(string key)
        {
            return data.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return data.TryGetValue(key, out value);
        }


        public object this[string key]
        {
            get
            {
                return GetValue(key);
            }
            set
            {
                SetValue(key, value);
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return data.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        [System.Xml.Serialization.XmlIgnore()]
        public int Count
        {
            get
            {
                return data.Count;
            }
        }

        [System.Xml.Serialization.XmlIgnore()]
        public bool IsReadOnly
        {
            get
            {
                return data.IsReadOnly;
            }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return data.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var item in data)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in data)
            {
                yield return item;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private static Dictionary<Type, Dictionary<string, System.Reflection.PropertyInfo>> _DynamicTypeProperties = new Dictionary<Type, Dictionary<string, System.Reflection.PropertyInfo>>();

        protected virtual void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                #region 缓存动态类型的属性
                //var type = this.GetType();
                //var item = _DynamicTypeProperties.GetDictionaryValue(type, null);
                //if (item == null)
                //{
                //    lock (_DynamicTypeProperties)
                //    {
                //        if (_DynamicTypeProperties.ContainsKey(type))
                //        {
                //            item = _DynamicTypeProperties[type];
                //        }
                //        else
                //        {
                //            var properties =
                //                type.GetProperties(System.Reflection.BindingFlags.Public)
                //                .ToDictionary((p) => p.Name);
                //            _DynamicTypeProperties[type] = properties;
                //            item = properties;
                //        }
                //    }
                //}
                #endregion

                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
                //if (item.ContainsKey(name))
                //{
                //    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs($"Item[]"));
                //}
            }
        }
        #endregion

        //只拷贝属性，属性值如果是引用还是引用（对象）
        public T Copy<T>() where T : IEntity, new()
        {
            var t = new T();
            foreach (var item in data)
            {
                t.SetValue(item.Key, item.Value);
            }
            return t;
        }

        public virtual string ToJson()
        {
            return JsonHelper.ToJson(this);
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
