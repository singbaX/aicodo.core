using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AiCodo.Data
{
    public class TagItemBase : EntityBase
    {
        #region 属性 Tag
        private string _Tag = string.Empty;
        [XmlAttribute("Tag"), DefaultValue("")]
        public string Tag
        {
            get
            {
                return _Tag;
            }
            set
            {
                if (_Tag == value)
                {
                    return;
                }
                _Tag = value;
                RaisePropertyChanged("Tag");
            }
        }
        #endregion

        #region 属性 Attrs
        private AttrCollection _Attrs = null;
        [XmlElement("Attr", typeof(AttrItem))]
        public AttrCollection Attrs
        {
            get
            {
                if (_Attrs == null)
                {
                    _Attrs = new AttrCollection();
                }
                return _Attrs;
            }
            set
            {
                _Attrs = value;
                RaisePropertyChanged("Attrs");
            }
        }
        #endregion
    }

    public class TagEntity : Entity
    {
        #region 属性 Tag
        private string _Tag = string.Empty;
        [XmlAttribute("Tag"), DefaultValue("")]
        public string Tag
        {
            get
            {
                return _Tag;
            }
            set
            {
                if (_Tag == value)
                {
                    return;
                }
                _Tag = value;
                RaisePropertyChanged("Tag");
            }
        }
        #endregion

        #region 属性 Attrs
        private AttrCollection _Attrs = null;
        [XmlElement("Attr", typeof(AttrItem))]
        public AttrCollection Attrs
        {
            get
            {
                if (_Attrs == null)
                {
                    _Attrs = new AttrCollection();
                }
                return _Attrs;
            }
            set
            {
                _Attrs = value;
                RaisePropertyChanged("Attrs");
            }
        }
        #endregion
    }


    public class AttrCollection : CollectionBase<AttrItem>
    {
        public string this[string name]
        {
            get
            {
                var item = Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return item == null ? "" : item.Value;
            }
            set
            {
                var item = Items.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    item = new AttrItem { Name = name, Value = value };
                    Items.Add(item);
                }
                else
                {
                    item.Value = value;
                }
            }
        }
    }

    public class AttrItem : EntityBase
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

        #region 属性 Value
        private string _Value = string.Empty;
        [XmlAttribute("Value"), DefaultValue("")]
        public string Value
        {
            get
            {
                return _Value;
            }
            set
            {
                if (_Value == value)
                {
                    return;
                }
                _Value = value;
                RaisePropertyChanged("Value");
            }
        }
        #endregion
    }
}
