// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using Newtonsoft.Json;
    using System;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    [JsonConverter(typeof(DynamicExpandoJsonConverter<DynamicEntity>))]
    public class DynamicEntity : DynamicExpando, IXmlSerializable
    {
        public DynamicEntity()
        {

        }

        public DynamicEntity(IEntity entity)
        {
            var nameValues = entity.GetNameValues();
            if (nameValues != null)
            {
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    SetValue(nameValues[i].ToString(), nameValues[i + 1]);
                }
            }
        }

        public DynamicEntity(params object[] nameValues)
        {
            if (nameValues != null)
            {
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    SetValue(nameValues[i].ToString(), nameValues[i + 1]);
                }
            }
        }

        public DynamicEntity Set(DynamicEntity d)
        {
            foreach (var p in d)
            {
                this.SetValue(p.Key, p.Value);
            }
            return this;
        }

        public DynamicEntity Set(string key, object value)
        {
            this.SetValue(key, value);
            return this;
        }

        public string GetString(string key, string value = "")
        {
            var v = base.GetValue(key, null);
            return v == null ? value :
                v is string ? (string)v :
                v.GetType().IsClass ? v.ToJson() : v.ToString();
        }

        public DateTime GetDate(string key, DateTime value)
        {
            var v = base.GetValue(key, null);
            return v.IsNull() ? value : Convert.ToDateTime(v);
        }

        public int GetInt32(string key, int value = 0)
        {
            var v = base.GetValue(key, null);
            return v.IsNullOrEmpty() ? value : v.ToInt32();
        }
        public long GetInt64(string key, long value = 0)
        {
            var v = base.GetValue(key, null);
            return v.IsNullOrEmpty() ? value : v.ToInt64();
        }

        public bool GetBool(string key, bool value = false)
        {
            var v = base.GetValue(key, null);
            return v.IsNullOrEmpty() ? value : v.ToBoolean();
        }

        public double GetDouble(string key, double value = 0.0)
        {
            var v = base.GetValue(key, null);
            return v.IsNullOrEmpty() ? value : v.ToDouble();
        }

        public T GetJsonDataValue<T>(string key)
        {
            var value = GetValue(key);
            if (value == null)
            {
                return default(T);
            }
            if (value is T)
            {
                return (T)value;
            }

            var json = value is string ? (value as string) : value.ToJson();
            return json.ToJsonObject<T>();
        }

        public override object GetValue(string key, object defaultValue = null)
        {
            lock (this)
            {
                var index = key.IndexOf('.');
                if (index > 0)
                {
                    var name = key.Substring(0, index);
                    var obj = base.GetValue(name, null);

                    var subName = key.Substring(index + 1);
                    if (obj is IEntity)
                    {
                        return (obj as IEntity).GetValue(subName, defaultValue);
                    }
                    else
                    {
                        var v = obj.GetPathValue(subName);
                        return v.IsNullOrEmpty() ? defaultValue : v;
                    }
                }
                else
                {
                    return base.GetValue(key, defaultValue);
                }
            }
        }

        public override void SetValue(string key, object value)
        {
            base.SetValue(key, value);

            lock (this)
            {
                var index = key.IndexOf('.');
                if (index > 0)
                {
                    var name = key.Substring(0, index);
                    var obj = base.GetValue(name, null);
                    if (obj == null)
                    {
                        obj = new DynamicEntity();
                        base.SetValue(name, obj);
                    }

                    var subName = key.Substring(index + 1);
                    if (obj is IEntity)
                    {
                        (obj as IEntity).SetValue(subName, value);
                    }
                    else
                    {
                        obj.SetPathValue(subName, value);
                    }
                }
                else
                {
                    base.SetValue(key, value);
                }
            }
        }

        #region 构造
        public static implicit operator DynamicEntity(string doc)
        {
            if (string.IsNullOrEmpty(doc))
            {
                return new DynamicEntity();
            }
            var json = doc.Trim();
            try
            {
                if (json.StartsWith("{") && json.EndsWith("}"))
                {
                    return json.ToJsonObject<DynamicEntity>();
                }
                else
                {
                    return CreateOfNameValues(json);
                }
            }
            catch
            {
                throw;
            }
        }

        private static DynamicEntity CreateOfNameValues(string json)
        {
            DynamicEntity item = new DynamicEntity();
            foreach (var kv in json.Split('&', ';'))
            {
                var values = kv.Split('=', ':');
                if (values.Length == 2)
                {
                    item.SetValue(values[0], values[1]);
                }
            }
            return item;
        }

        public static implicit operator string(DynamicEntity obj)
        {
            return obj.ToJson();
        }
        #endregion

        #region IXmlSerializable
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            var text = reader.ReadInnerXml();
            DynamicEntity innerData = text;
            innerData.ForEach(item => this.SetValue(item.Key, item.Value));
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteString(this.ToFormatJson());
        }
        #endregion
    }
}
