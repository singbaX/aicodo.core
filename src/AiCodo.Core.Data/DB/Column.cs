// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AiCodo.Data
{
    public class Column : TagEntity
    {
        public static string[] DBFields = new string[]
        {
           "Name", "ColumnType","DataType","Length","IsKey","NullAble","IsAutoIncrement","ColumnOrdinal","PropertyName","IsReadOnly"
        };

        public static bool IsDBFields(string name)
        {
            return DBFields.FirstOrDefault(f => f.Equals(name, StringComparison.OrdinalIgnoreCase)) != null;
        }

        #region 属性 Name
        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                return GetFieldValue<string>("Name", string.Empty);
            }
            set
            {
                SetFieldValue("Name", value);
            }
        }
        #endregion

        #region 属性 DisplayName
        [XmlAttribute("DisplayName"), DefaultValue("")]
        public string DisplayName
        {
            get
            {
                return GetFieldValue<string>("DisplayName", string.Empty);
            }
            set
            {
                SetFieldValue("DisplayName", value);
            }
        }
        #endregion

        #region property Comment 
        [XmlAttribute("Comment"), DefaultValue("")]
        public string Comment
        {
            get
            {
                return GetFieldValue<string>("Comment", string.Empty);
            }
            set
            {
                SetFieldValue("Comment", value);
            }
        }
        #endregion

        #region 属性 ColumnOrdinal
        [XmlAttribute("ColumnOrdinal")]
        public int ColumnOrdinal
        {
            get
            {
                return GetFieldValue<int>("ColumnOrdinal", 0);
            }
            set
            {
                SetFieldValue("ColumnOrdinal", value);
            }
        }
        #endregion

        #region 属性 ColumnType
        /// <summary>
        /// 数据库的字段类型
        /// </summary>
        [XmlAttribute("ColumnType")]
        public string ColumnType
        {
            get
            {
                return GetFieldValue<string>("ColumnType", string.Empty);
            }
            set
            {
                SetFieldValue("ColumnType", value);
            }
        }
        #endregion

        #region 属性 DataType
        /// <summary>
        /// 字段的值类型
        /// </summary>
        [XmlAttribute("DataType")]
        public string DataType
        {
            get
            {
                return GetFieldValue<string>("DataType", string.Empty);
            }
            set
            {
                SetFieldValue("DataType", value);
            }
        }
        #endregion

        #region 属性 DbType
        /// <summary>
        /// 对应标准的DbType
        /// </summary>
        [XmlAttribute("DbType")]
        public System.Data.DbType DbType
        {
            get
            {
                return GetFieldValue<System.Data.DbType>("DbType", System.Data.DbType.String);
            }
            set
            {
                SetFieldValue("DbType", value);
            }
        }
        #endregion

        #region 属性 PropertyType
        /// <summary>
        /// 实体类的字段类型
        /// </summary>
        [XmlAttribute("PropertyType")]
        public string PropertyType
        {
            get
            {
                return GetFieldValue<string>("PropertyType", string.Empty);
            }
            set
            {
                SetFieldValue("PropertyType", value);
            }
        }
        #endregion

        #region 属性 PropertyName
        [XmlAttribute("PropertyName")]
        public string PropertyName
        {
            get
            {
                return GetFieldValue<string>("PropertyName", string.Empty);
            }
            set
            {
                SetFieldValue("PropertyName", value);
            }
        }
        #endregion

        #region 属性 Remark
        [XmlAttribute("Remark")]
        public string Remark
        {
            get
            {
                return GetFieldValue<string>("Remark", string.Empty);
            }
            set
            {
                SetFieldValue("Remark", value);
            }
        }
        #endregion

        #region 属性 DefaultValue
        [XmlAttribute("DefaultValue")]
        public string DefaultValue
        {
            get
            {
                return GetFieldValue<string>("DefaultValue", string.Empty);
            }
            set
            {
                SetFieldValue("DefaultValue", value);
            }
        }
        #endregion

        #region 属性 Length
        [XmlAttribute("Length")]
        public long Length
        {
            get
            {
                return GetFieldValue<long>("Length", 0);
            }
            set
            {
                SetFieldValue("Length", value);
            }
        }
        #endregion

        #region 属性 NullAble
        [XmlAttribute("NullAble")]
        public bool NullAble
        {
            get
            {
                return GetFieldValue<bool>("NullAble", false);
            }
            set
            {
                SetFieldValue("NullAble", value);
            }
        }
        #endregion

        #region 属性 IsAutoIncrement
        [XmlAttribute("IsAutoIncrement")]
        public bool IsAutoIncrement
        {
            get
            {
                return GetFieldValue<bool>("IsAutoIncrement", false);
            }
            set
            {
                SetFieldValue("IsAutoIncrement", value);
            }
        }
        #endregion

        #region 属性 IsReadOnly
        /// <summary>
        /// 只有新增时才可以赋值，不能更新
        /// </summary>
        [XmlAttribute("IsReadOnly")]
        public bool IsReadOnly
        {
            get
            {
                return GetFieldValue<bool>("IsReadOnly", false);
            }
            set
            {
                SetFieldValue("IsReadOnly", value);
            }
        }
        #endregion

        #region 属性 IsKey
        [XmlAttribute("IsKey")]
        public bool IsKey
        {
            get
            {
                return GetFieldValue<bool>("IsKey", false);
            }
            set
            {
                SetFieldValue("IsKey", value);
            }
        }
        #endregion

        #region 属性 IsSystem
        [XmlAttribute("IsSystem")]
        public bool IsSystem
        {
            get
            {
                return GetFieldValue<bool>("IsSystem", false);
            }
            set
            {
                SetFieldValue("IsSystem", value);
            }
        }
        #endregion

        #region 属性 SystemParameter
        [XmlAttribute("SystemParameter")]
        public string SystemParameter
        {
            get
            {
                return GetFieldValue<string>("SystemParameter", string.Empty);
            }
            set
            {
                SetFieldValue("SystemParameter", value);
            }
        }
        #endregion

        public void ResetComment(string comment)
        {
            comment = comment.Trim();
            var tindex = comment.IndexOfAny(new char[] { ':', '：', '\r', '\n' });
            if (tindex > 0)
            {
                var displayName = comment.Substring(0, tindex);
                comment = comment.Substring(tindex + 1).TrimStart(' ', ':', '：', '\r', '\n');
                if (DisplayName.IsNullOrEmpty())
                {
                    DisplayName = displayName;
                }
                if (Comment.IsNullOrEmpty())
                {
                    Comment = comment;
                }                
            }
            else
            {
                if (DisplayName.IsNullOrEmpty())
                {
                    DisplayName = comment;
                }
            }
        }
    }
}
