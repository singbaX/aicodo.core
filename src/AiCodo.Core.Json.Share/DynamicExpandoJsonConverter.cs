using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

#if Newton
using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace AiCodo
{
#if Newton
    public class DynamicExpandoJsonConverter<T> : JsonConverter where T : DynamicExpando, new()
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = null;
            try
            {
                jsonObject = JObject.Load(reader);
                return CreateDynamicExpando(jsonObject);
            }
            catch (Exception ex)
            {
                return default(T);
            }
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
                        List<object> items = ReadArray(p);
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
                            var pvalue = (value as JValue).Value;
                            if (pvalue is long)
                            {
                                if ((long)pvalue < int.MaxValue && (long)pvalue > int.MinValue)
                                {
                                    d.SetValue(p.Name, Convert.ToInt32(pvalue));
                                    break;
                                }
                            }
                            d.SetValue(p.Name, pvalue);
                        }
                        else
                        {
                            d.SetValue(p.Name, p.Value.ToString());
                        }
                        break;
                }
            }
            return d;
        }

        private static List<object> ReadArray(JProperty p)
        {
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

            return items;
        }


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var d = value as T;

            writer.WriteStartObject();
            foreach (var name in d.GetDynamicMemberNames())
            {
                writer.WritePropertyName(name);
                serializer.Serialize(writer, d.GetValue(name));
            }
            writer.WriteEndObject();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
