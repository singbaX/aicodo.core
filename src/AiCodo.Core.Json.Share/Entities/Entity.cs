// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    /// <summary>
    ///     数据实体对象定义
    /// </summary>
    public class Entity : StaticExpando
    {
        public virtual T GetFieldValue<T>(string name, T defaultValue = default(T))
        {
            object value = GetValue(name);

            if (value != null && value is T)
            {
                return (T)value;
            }
            if (defaultValue == null)
            {
                return defaultValue;
            }

            if (value == null)
            {
                return defaultValue;
            }

            var newValue = value.GetConverterValue<T>();

            SetValue(name, newValue);

            return newValue;
        }

        public virtual void SetFieldValue(string name, object value)
        {
            SetValue(name, value);
        }

        /// <summary>
        ///     更新整个函数
        /// </summary>
        /// <param name="newVlaue"></param>
        public virtual void SetValue(Entity newVlaue)
        {
            foreach (string name in newVlaue.GetFieldNames())
            {
                object value = newVlaue.GetValue(name);
                if (!GetValue(name).Equals(value))
                {
                    SetValue(name, value);
                }
            }
        }

    }

    /*int
string
long
DateTime
bool
System.Byte[]
double*/

    public static class EntityExtends
    {
        private static readonly Dictionary<Type, Func<object, object>> ValueConverters =
            new Dictionary<Type, Func<object, object>>();

        static EntityExtends()
        {
            AddConverter(typeof(bool), value => value.ToBoolean());
            AddConverter(typeof(decimal), value => value.ToDecimal());
            AddConverter(typeof(double), value => value.ToDouble());
            AddConverter(typeof(float), value => value == null ? 0 : ((value is float) ? (float)value : Convert.ToSingle(value)));
            AddConverter(typeof(int), value => value.ToInt32());
            AddConverter(typeof(long), value => value.ToInt64());
            AddConverter(typeof(uint),value => value.ToUInt32());
            AddConverter(typeof(ulong),value => value.ToUInt64());
            AddConverter(typeof(DateTime),value => value.ToDateTime());
            AddConverter(typeof(string),value => value == null? string.Empty: ((value is string) ? (string)value : value.ToString()));
            AddConverter(typeof(DynamicEntity),
                         value =>
                         {
                             if (value == null)
                             {
                                 return new DynamicEntity();
                             }
                             DynamicEntity d = ((value is string) ? (string)value : value.ToString());
                             return d;
                         });
        }

        private static byte[] GetBytes(object value)
        {
            if (value == null)
            {
                return new byte[0];
            }
            if (value is byte[])
            {
                return (byte[])value;
            }
            if (value is string)
            {
                return Convert.FromBase64String(value.ToString());
            }
            if (value is Guid)
            {
                return ((Guid)value).ToByteArray();
            }
            return null;
        }

        public static void AddConverter(this Type type, Func<object, object> converter)
        {
            ValueConverters[type] = converter;
        }

        public static object GetConverterValue(this Type type, object value)
        {
            if (value == null)
            {
                return default;
            }

            Func<object, object> converter = null;
            if (value.GetType() == type)
            {
                return value;
            }

            if (ValueConverters.TryGetValue(type, out converter))
            {
                return converter(value);
            }

            if (type.IsEnum)
            {
                return (value is int) ? value : Enum.Parse(type, value.ToString(), true);
            }

            if (value == null && type.IsValueType == false)
            {
                return default;
            }
            else
            {
                try
                {
                    var json = value.ToJson();
                    return json.ToJsonObject(type);
                }
                catch
                {
                    throw new ArgumentOutOfRangeException("value",
                                                      string.Format("值{0}与类型{1}不匹配", value, type.FullName));
                }
            }
        }

        public static T GetConverterValue<T>(this object value)
        {
            Func<object, object> converter = null;
            if (value is T)
            {
                return (T)value;
            }
            if (ValueConverters.TryGetValue(typeof(T), out converter))
            {
                return (T)converter(value);
            }

            if (typeof(T).IsEnum)
            {
                return (value is int) ? (T)value : (T)Enum.Parse(typeof(T), value.ToString(), true);
            }

            if (value == null && typeof(T).IsValueType == false)
            {
                return default(T);
            }
            else
            {
                try
                {
                    var json = value.ToJson();
                    return json.ToJsonObject<T>();
                }
                catch
                {
                    throw new ArgumentOutOfRangeException("value",
                                                      string.Format("值{0}与类型{1}不匹配", value, typeof(T).FullName));
                }
            }
        }
    }
}
