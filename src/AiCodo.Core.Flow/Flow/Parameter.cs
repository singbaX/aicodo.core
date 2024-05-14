using AiCodo.Flow.Configs;
using System.ComponentModel;
using System.Xml.Serialization;

namespace AiCodo.Flow
{
    public enum ParameterType
    {
        Any,
        Option,
        Bool,
        Int,
        Long,
        Single,
        Double,
        String,
        Date,
        Time,
        DateTime,
        Object,
        List,
        Image,//图片路径
        FilePath,//文件路径
    }

    public partial class ResultParameter : ParameterBase
    {

    }

    public partial class ParameterItem : ParameterBase
    {
        #region 属性 Max
        private string _Max = string.Empty;
        /// <summary>
        /// 最大值
        /// </summary>
        [XmlAttribute("Max"), DefaultValue("")]
        public string Max
        {
            get
            {
                return _Max;
            }
            set
            {
                _Max = value;
                RaisePropertyChanged("Max");
            }
        }
        #endregion

        #region 属性 Min
        private string _Min = string.Empty;
        /// <summary>
        /// 最小值
        /// </summary>
        [XmlAttribute("Min"), DefaultValue("")]
        public string Min
        {
            get
            {
                return _Min;
            }
            set
            {
                _Min = value;
                RaisePropertyChanged("Min");
            }
        }
        #endregion

        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        /// <summary>
        /// 默认值
        /// </summary>
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
                RaisePropertyChanged("DefaultValue");
            }
        }
        #endregion
    }

    public partial class ParameterBase : ConfigItemBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        /// <summary>
        /// 参数名称（一旦使用不能修改）
        /// </summary>
        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 DisplayName
        private string _DisplayName = string.Empty;
        /// <summary>
        /// 显示名称（界面显示名称）
        /// </summary>
        [XmlAttribute("DisplayName")]
        public string DisplayName
        {
            get
            {
                if (_DisplayName.IsNullOrEmpty())
                {
                    return Name;
                }
                return _DisplayName;
            }
            set
            {
                _DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        #endregion

        #region 属性 Type
        private ParameterType _Type = ParameterType.Any;
        /// <summary>
        /// 参数类型：int,string,image,imagelist
        /// </summary>
        [XmlIgnore]
        public ParameterType Type
        {
            get
            {
                if (_Type == ParameterType.Any)
                {
                    _Type = GetParameterType();
                }
                return _Type;
            }
            set
            {
                _Type = value;
                RaisePropertyChanged("Type");
            }
        }
        #endregion

        #region 属性 TypeName
        private string _TypeName = string.Empty;
        [XmlAttribute("TypeName"), DefaultValue("")]
        public string TypeName
        {
            get
            {
                return _TypeName;
            }
            set
            {
                _TypeName = value;
                RaisePropertyChanged("TypeName");
                Type = GetParameterType();
            }
        }

        private ParameterType GetParameterType()
        {
            return GetParameterTypeOfName(TypeName);
        }

        public ParameterType GetParameterTypeOfName(string typeName)
        {
            var type = ParameterType.Any;
            if (typeName.IsNotEmpty())
            {
                if (ConfigRoot is FunctionConfig config)
                {
                }
                else
                {
                    config = FunctionConfig.Current;
                }
                var typeItem = config.GetType(typeName);
                if (typeItem != null)
                {
                    type = typeItem.Type;
                }
            }

            return type;
        }
        #endregion

        #region 属性 IsEnabled
        private bool _IsEnabled = true;
        /// <summary>
        /// 是否有效
        /// </summary>
        [XmlAttribute("IsEnabled"), DefaultValue(true)]
        public bool IsEnabled
        {
            get
            {
                return _IsEnabled;
            }
            set
            {
                _IsEnabled = value;
                RaisePropertyChanged("IsEnabled");
            }
        }
        #endregion

        public object GetValue(object value)
        {
            return value.ConvertToParameterValue(Type);
        }
    }
}
