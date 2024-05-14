using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace AiCodo.Data
{
    public class FilterItem : EntityBase
    {
        #region 属性 Name
        private string _Name = string.Empty;
        [XmlAttribute("Name"),DefaultValue("")]
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

        #region 属性 Type
        private string _Type = "";
        [XmlAttribute("Type")]
        public string Type 
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
                RaisePropertyChanged("Type");
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
                _Value = value;
                RaisePropertyChanged("Value");
            }
        }
        #endregion

        #region 属性 Items
        private CollectionBase<FilterItem> _Items = null;
        [XmlElement("Item",typeof(FilterItem))]
        public CollectionBase<FilterItem> Items
        {
            get
            {
                if(_Items == null)
                {
                    _Items = new CollectionBase<FilterItem>();
                }
                return _Items;
            }
            set
            {
                _Items = value;
                RaisePropertyChanged("Items");
            }
        }
        #endregion
    }
}
