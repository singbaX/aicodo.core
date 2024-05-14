// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [JsonConverter(typeof(RowEntityJsonConverter<RowEntity>))]
    public class RowEntity : Entity
    {
        public object this[string name]
        {
            get
            {
                return base.GetValue(name, null);
            }
            set
            {
                base.SetValue(name, value);
            }
        }

        public static RowEntity Create(IEntity entity, params string[] names)
        {
            var row = new RowEntity();
            names = entity.GetFieldNames().ToArray();
            foreach (var name in names)
            {
                row.SetFieldValue(name, entity.GetValue(name, null));
            }
            return row;
        }
    }


    public class RowEntityJsonConverter<T> : JsonConverter where T : RowEntity, new()
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            return CreateDynamicExpando(jsonObject);
        }

        private static object CreateDynamicExpando(JObject jsonObject)
        {
            var properties = jsonObject.Properties().ToList();
            var d = new T();
            foreach (var p in properties)
            {
                switch (p.Value.Type)
                {
                    case JTokenType.Array:
                        List<object> items = new List<object>();
                        var values = p.Value as JArray;
                        foreach (var pitem in values)
                        {
                            if (pitem is JValue)
                            {
                                items.Add(pitem);
                            }
                            else if (pitem is JObject)
                            {
                                var ditem = CreateDynamicExpando(pitem as JObject);
                                items.Add(ditem);
                            }
                        }
                        d.SetValue(p.Name, items);
                        break;
                    case JTokenType.Object:
                        d.SetValue(p.Name, CreateDynamicExpando(p.Value as JObject));
                        break;
                    //case JTokenType.Constructor: 
                    //case JTokenType.None:
                    //case JTokenType.Property:
                    //case JTokenType.Comment:
                    //case JTokenType.Integer:
                    //case JTokenType.Float:
                    //case JTokenType.String:
                    //case JTokenType.Boolean:
                    //case JTokenType.Null:
                    //case JTokenType.Undefined:
                    //case JTokenType.Date:
                    //case JTokenType.Raw:
                    //case JTokenType.Bytes:
                    //case JTokenType.Guid:
                    //case JTokenType.Uri:
                    //case JTokenType.TimeSpan:
                    default:
                        var value = p.Value;
                        if (value is JValue)
                        {
                            d.SetValue(p.Name, (value as JValue).Value);
                        }
                        else
                        {
                            if (value == null)
                            {
                                d.SetValue(p.Name, null);
                            }
                            else
                            {
                                d.SetValue(p.Name, value.ToJson());
                            }
                        }
                        break;
                }
            }
            return d;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var d = value as T;

            writer.WriteStartObject();
            foreach (var name in d.GetFieldNames())
            {
                writer.WritePropertyName(name);
                var vv = d.GetValue(name, null);
                if (vv != null && vv is DateTime time)
                {
                    vv = $"{time.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}+08:00";
                }
                serializer.Serialize(writer, vv);
            }
            writer.WriteEndObject();
        }
    }
}
