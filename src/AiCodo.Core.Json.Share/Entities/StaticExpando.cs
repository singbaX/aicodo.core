// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Xml.Serialization;

    public class StaticExpando : INotifyPropertyChanged, IEntity
    {
        private readonly IDictionary<string, object> data = new Dictionary<string, object>();
        private readonly object datalock = new object();

        #region INotifyPropertyChanged Members
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        protected virtual void RaisePropertyChanged<TProperty>(Expression<Func<TProperty>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else memberExpression = (MemberExpression)lambda.Body;
            RaisePropertyChanged(memberExpression.Member.Name);
        }
        #endregion

        public virtual IEnumerable<string> GetFieldNames()
        {
            return data.Keys;
        }

        [XmlIgnore,JsonIgnore]
        public IEnumerable<string> Keys
        {
            get
            {
                return data.Keys;
            }
        }

        [XmlIgnore,JsonIgnore]
        public int FieldCount
        {
            get
            {
                return data.Count;
            }
        }

        protected bool HasField(string name)
        {
            return data.ContainsKey(name);
        }

        public virtual object[] GetNameValues()
        {
            return data.ToNameValues();
        }

        public void SetValue(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
#if EntityLock
            lock (datalock)
            {
                data[key] = value;
                RaisePropertyChanged(key);
            }
#else
            data[key] = value;
            RaisePropertyChanged(key);
#endif 
        }

        public object GetValue(string key, object defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
#if EntityLock
            lock (datalock)
            {
                object dataValue;
                if (data.TryGetValue(key, out dataValue))
                {
                }
                else
                {
                    data[key] = defaultValue;
                    dataValue = defaultValue;
                }
                return dataValue;
            }
#else
            object dataValue;
            if (data.TryGetValue(key, out dataValue))
            {
            }
            else
            {
                data[key] = defaultValue;
                dataValue = defaultValue;
            }
            return dataValue;
#endif
        }

        public virtual void RemoveKey(string key)
        {
            if (data.ContainsKey(key))
            {
                data.Remove(key);
            }
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
