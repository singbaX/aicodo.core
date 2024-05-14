// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Codes
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;

    [XmlRoot("CodeSetting")]
    public class CodeSetting : EntityBase
    {
        const string CodeSettingName = "CodeSetting";

        static string _ConfigFileName = $"{CodeSettingName}.xml".FixedAppConfigPath();

        private static object _LoadLock = new object();

        private Dictionary<string, Dictionary<string, TypeMapping>> _TypeMappingItems = new Dictionary<string, Dictionary<string, TypeMapping>>();
        private Dictionary<string, ParameterType> _GlobalParametersItems = new Dictionary<string, ParameterType>();

        static CodeSetting()
        {

        }

        #region 属性 Current
        private static CodeSetting _Current = null;
        public static CodeSetting Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_LoadLock)
                    {
                        if (_Current == null)
                        {
                            ReloadCurrent();
                        }
                    }
                }
                return _Current;
            }
        }

        public static void ReloadCurrent()
        {
            var fileName = _ConfigFileName;
            CodeSetting setting;
            if (fileName.IsFileExists())
            {
                setting = fileName.LoadXDoc<CodeSetting>();
                setting.ResetTypeMappingItems();
            }
            else
            {
                setting = new CodeSetting();
            }
            setting.Log($"加载代码配置文件[{fileName}]");
            _Current = setting;
        }
        #endregion

        #region 属性 Namespace
        private string _Namespace = "AiCodo";
        [XmlAttribute]
        public string Namespace
        {
            get
            {
                return _Namespace;
            }
            set
            {
                _Namespace = value;
                RaisePropertyChanged("Namespace");
            }
        }
        #endregion

        #region 属性 IgnoreColumns
        private string _IgnoreColumns = string.Empty;
        [XmlElement("IgnoreColumns")]
        public string IgnoreColumns
        {
            get
            {
                return _IgnoreColumns;
            }
            set
            {
                _IgnoreColumns = value;
                RaisePropertyChanged("IgnoreColumns");
            }
        }
        #endregion 

        #region 属性 CommandMappings
        private CollectionBase<CommandMapping> _CommandMappings = null;
        [XmlElement("Command", typeof(CommandMapping))]
        public CollectionBase<CommandMapping> CommandMappings
        {
            get
            {
                if (_CommandMappings == null)
                {
                    _CommandMappings = new CollectionBase<CommandMapping>();
                }
                return _CommandMappings;
            }
            set
            {
                _CommandMappings = value;
                RaisePropertyChanged("CommandMappings");
            }
        }
        #endregion 

        #region 属性 GlobalParameters
        private CollectionBase<ParameterType> _GlobalParameters = null;
        [XmlArray("GlobalParameters")]
        [XmlArrayItem("Paramter", typeof(ParameterType))]
        public CollectionBase<ParameterType> GlobalParameters
        {
            get
            {
                if (_GlobalParameters == null)
                {
                    _GlobalParameters = new CollectionBase<ParameterType>();
                }
                return _GlobalParameters;
            }
            set
            {
                _GlobalParameters = value;
                RaisePropertyChanged(() => GlobalParameters);
            }
        }
        #endregion

        #region 属性 TypeMappings
        private CollectionBase<TypeMapping> _TypeMappings = null;
        [XmlArray("TypeMappings")]
        [XmlArrayItem("Item", typeof(TypeMapping))]
        public CollectionBase<TypeMapping> TypeMappings
        {
            get
            {
                if (_TypeMappings == null)
                {
                    _TypeMappings = new CollectionBase<TypeMapping>();
                }
                return _TypeMappings;
            }
            set
            {
                _TypeMappings = value;
                RaisePropertyChanged("TypeMappings");
                ResetTypeMappingItems();
            }
        }
        #endregion

        #region 属性 Templates
        private CollectionBase<CodeTemplateItem> _Templates = null;
        [XmlArray("Templates")]
        [XmlArrayItem("Item", typeof(CodeTemplateItem))]
        public CollectionBase<CodeTemplateItem> Templates
        {
            get
            {
                if (_Templates == null)
                {
                    _Templates = new CollectionBase<CodeTemplateItem>();
                }
                return _Templates;
            }
            set
            {
                _Templates = value;
                RaisePropertyChanged("Templates");
            }
        }
        #endregion

        #region 属性 Commands
        private CollectionBase<CodeCommandItem> _Commands = null;
        [XmlArray("Commands")]
        [XmlArrayItem("Item", typeof(CodeCommandItem))]
        public CollectionBase<CodeCommandItem> Commands
        {
            get
            {
                if (_Commands == null)
                {
                    _Commands = new CollectionBase<CodeCommandItem>();
                }
                return _Commands;
            }
            set
            {
                _Commands = value;
                RaisePropertyChanged("Commands");
            }
        }
        #endregion

        #region GetParameter
        public string GetParameterCodeType(string parameterName)
        {
            if (_GlobalParametersItems.TryGetValue(parameterName.ToLower(), out ParameterType item))
            {
                return item.CodeType;
            }
            return "";
        }

        public string GetParameterDefaultValue(string parameterName)
        {
            if (_GlobalParametersItems.TryGetValue(parameterName.ToLower(), out ParameterType item))
            {
                return item.DefaultValue;
            }
            return "";
        }

        private void ResetGlobalParametersItems()
        {
            GlobalParameters.ForEach(g =>
                {
                    _GlobalParametersItems[g.Name.ToLower()] = g;
                });
        }

        #endregion

        #region datatype mapping

        public string GetCodeType(string dataType, string providerName = "")
        {
            TypeMapping item = GetDataTypeMappingItem(dataType, providerName);
            if (item != null)
            {
                return item.CodeType;
            }
            return "";
        }

        public string GetDefaultValue(string dataType, string providerName = "")
        {
            TypeMapping item = GetDataTypeMappingItem(dataType, providerName);
            if (item != null)
            {
                return item.DefaultValue;
            }
            return "";
        }
        private void ResetTypeMappingItems()
        {
            TypeMappings.GroupBy(t => t.ProviderName.ToLower())
                .ForEach(g =>
                {
                    _TypeMappingItems[g.Key] = g.ToDictionary(d => d.DataType.ToLower());
                });
        }

        private TypeMapping GetDataTypeMappingItem(string dataType, string providerName)
        {
            TypeMapping item = null;
            if (_TypeMappingItems.TryGetValue(providerName.ToLower(), out var list))
            {
                if (providerName.IsNotEmpty())
                {
                    if (list.TryGetValue(dataType.ToLower(), out item))
                    {
                        return item;
                    }
                }
            }

            return item;
        }
        #endregion
    }

    public class CommandMapping : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name == value)
                {
                    return;
                }
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 FormatLine
        private string _FormatLine = string.Empty;
        [XmlAttribute("FormatLine"), DefaultValue("")]
        public string FormatLine
        {
            get
            {
                return _FormatLine;
            }
            set
            {
                if (_FormatLine == value)
                {
                    return;
                }
                _FormatLine = value;
                RaisePropertyChanged("FormatLine");
            }
        }
        #endregion
    }

    public class CodeCommandItem : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 DisplayName
        private string _DisplayName = string.Empty;
        [XmlAttribute("DisplayName"), DefaultValue("")]
        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                if (_DisplayName == value)
                {
                    return;
                }
                _DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        #endregion

        #region 属性 FileName
        private string _FileName = string.Empty;
        [XmlAttribute("FileName"), DefaultValue("")]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                if (_FileName == value)
                {
                    return;
                }
                _FileName = value;
                RaisePropertyChanged("FileName");
            }
        }
        #endregion

        #region 属性 ArgNames
        private string _ArgNames = string.Empty;
        [XmlAttribute("ArgNames"), DefaultValue("")]
        public string ArgNames
        {
            get
            {
                return _ArgNames;
            }
            set
            {
                if (_ArgNames == value)
                {
                    return;
                }
                _ArgNames = value;
                RaisePropertyChanged("ArgNames");
            }
        }
        #endregion
    }

    public class CodeTemplateItem : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged(() => Name);
            }
        }
        #endregion

        #region 属性 DisplayName
        private string _DisplayName = string.Empty;
        [XmlAttribute("DisplayName"), DefaultValue("")]
        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                if (_DisplayName == value)
                {
                    return;
                }
                _DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        #endregion

        #region 属性 ModelType
        private string _ModelType = string.Empty;
        [XmlAttribute("ModelType"), DefaultValue("")]
        public string ModelType
        {
            get
            {
                return _ModelType;
            }
            set
            {
                _ModelType = value;
                RaisePropertyChanged("ModelType");
            }
        }
        #endregion

        #region 属性 FileName
        private string _FileName = string.Empty;
        [XmlAttribute("FileName"), DefaultValue("")]
        public string FileName
        {
            get
            {
                return _FileName;
            }
            set
            {
                if (_FileName == value)
                {
                    return;
                }
                _FileName = value;
                RaisePropertyChanged("FileName");
            }
        }
        #endregion

        #region 属性 CodeFileName
        private string _CodeFileName = string.Empty;
        [XmlAttribute("CodeFileName"), DefaultValue("")]
        public string CodeFileName
        {
            get
            {
                return _CodeFileName;
            }
            set
            {
                _CodeFileName = value;
                RaisePropertyChanged(() => CodeFileName);
            }
        }
        #endregion
    }

    public class ParameterType : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"), DefaultValue("")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name == value)
                {
                    return;
                }
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 CodeType
        private string _CodeType = string.Empty;
        [XmlAttribute("CodeType"), DefaultValue("")]
        public string CodeType
        {
            get
            {
                return _CodeType;
            }
            set
            {
                if (_CodeType == value)
                {
                    return;
                }
                _CodeType = value;
                RaisePropertyChanged("CodeType");
            }
        }
        #endregion

        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        [XmlAttribute("DefaultValue"), DefaultValue("")]
        public string DefaultValue
        {
            get
            {
                return _DefaultValue;
            }
            set
            {
                if (_DefaultValue == value)
                {
                    return;
                }
                _DefaultValue = value;
                RaisePropertyChanged("DefaultValue");
            }
        }
        #endregion
    }

    public class TypeMapping : EntityBase
    {
        #region 属性 ProviderName
        private string _ProviderName = string.Empty;
        [XmlAttribute("ProviderName"), DefaultValue("")]
        public string ProviderName
        {
            get
            {
                return _ProviderName;
            }
            set
            {
                _ProviderName = value;
                RaisePropertyChanged(() => ProviderName);
            }
        }
        #endregion

        #region 属性 DataType
        private string _DataType = string.Empty;
        [XmlAttribute("DataType"), DefaultValue("")]
        public string DataType //DataType
        {
            get
            {
                return _DataType;
            }
            set
            {
                _DataType = value;
                RaisePropertyChanged(() => DataType);
            }
        }
        #endregion

        #region 属性 CodeType
        private string _CodeType = string.Empty;
        [XmlAttribute("CodeType"), DefaultValue("")]
        public string CodeType
        {
            get
            {
                return _CodeType;
            }
            set
            {
                _CodeType = value;
                RaisePropertyChanged(() => CodeType);
            }
        }
        #endregion

        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        [XmlAttribute("DefaultValue"), DefaultValue("")]
        public string DefaultValue
        {
            get
            {
                return _DefaultValue;
            }
            set
            {
                _DefaultValue = value;
                RaisePropertyChanged(() => DefaultValue);
            }
        }
        #endregion
    }
}
