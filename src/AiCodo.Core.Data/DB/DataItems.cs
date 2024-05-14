// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Data
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Xml.Serialization;
    [XmlRoot("DataItems")]
    public class DataItems : ConfigFile
    {
        Dictionary<string, DataItem> _AllItems = new Dictionary<string, DataItem>();

        #region 属性 Current 
        private static DataItems _Current = null;
        private static object _LoadLock = new object();
        public static DataItems Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_LoadLock)
                    {
                        if (_Current == null)
                        {
                            _Current = CreateOrLoad<DataItems>("DataItemsConfig.xml");
                        }
                    }
                }
                return _Current;
            }
        }
        #endregion

        #region 属性 Items
        private CollectionBase<DataItem> _Items = null;
        [XmlArray("Items"), XmlArrayItem("Item", typeof(DataItem))]
        public CollectionBase<DataItem> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new CollectionBase<DataItem>();
                    _Items.CollectionChanged += Items_CollectionChanged;
                }
                return _Items;
            }
            set
            {
                if (_Items != null)
                {
                    _Items.CollectionChanged -= Items_CollectionChanged;
                    OnItemsRemoved(_Items);
                }
                _Items = value;
                RaisePropertyChanged("Items");
                if (_Items != null)
                {
                    _Items.CollectionChanged += Items_CollectionChanged;
                    OnItemsAdded(_Items);
                }
                ResetAllItems();
            }
        }

        private void ResetAllItems()
        {
            lock (_AllItems)
            {
                _AllItems.Clear();
                Items.ForEach(item =>
                {
                    _AllItems[item.Name.ToLower()] = item;
                });
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnItemsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnItemsRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnItemsAdded(IList newItems)
        {
            foreach (DataItem item in newItems)
            {
                item.Parent = this;
            }
        }


        protected virtual void OnItemsRemoved(IList oldItems)
        {
            foreach (DataItem item in oldItems)
            {
                item.Parent = null;
            }
        }

        public DataItem GetItem(string codeName)
        {
            if (_AllItems.TryGetValue(codeName.ToLower(), out var item))
            {
                return item;
            }
            return null;
        }

        internal void AddItem(DataItem item)
        {
            lock (item)
            {
                Items.Add(item);
            }
        }

        internal void ItemNameChanged(DataItem item, string oldValue)
        {
            lock (_AllItems)
            {
                oldValue = oldValue.ToLower();
                if (_AllItems.TryGetValue(oldValue, out var dataItem))
                {
                    if (dataItem != item)
                    {
                        return;
                    }
                    _AllItems.Remove(oldValue);
                    _AllItems[item.Name.ToLower()] = dataItem;
                }
            }
        }
        #endregion
    }

    public class DataItem : EntityBase
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
                var oldValue = _Name;
                _Name = value;
                RaisePropertyChanged("Name");
                Parent?.ItemNameChanged(this, oldValue);
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

        #region 属性 BaseOn
        private string _BaseOn = string.Empty;
        [XmlAttribute("BaseOn"), DefaultValue("")]
        public string BaseOn
        {
            get
            {
                return _BaseOn;
            }
            set
            {
                if (_BaseOn == value)
                {
                    return;
                }
                _BaseOn = value;
                RaisePropertyChanged("BaseOn");
            }
        }
        #endregion

        #region 属性 Columns
        private CollectionBase<DataColumn> _Columns = null;
        [XmlElement("Column", typeof(DataColumn))]
        public CollectionBase<DataColumn> Columns
        {
            get
            {
                if (_Columns == null)
                {
                    _Columns = new CollectionBase<DataColumn>();
                }
                return _Columns;
            }
            set
            {
                _Columns = value;
                RaisePropertyChanged("Columns");
            }
        }
        #endregion

        #region 属性 Parent
        private DataItems _Parent = null;
        [XmlIgnore, JsonIgnore]
        public DataItems Parent
        {
            get
            {
                return _Parent;
            }
            set
            {
                _Parent = value;
                RaisePropertyChanged("Parent");
            }
        }
        #endregion

        public IEnumerable<DataColumn> GetColumns(bool includeBase = true)
        {
            if (BaseOn.IsNotEmpty())
            {
                if (Parent != null)
                {
                    var dataItem = Parent.GetItem(BaseOn);
                    if (dataItem != null)
                    {
                        foreach (var c in dataItem.GetColumns())
                        {
                            yield return c;
                        }
                    }
                }
            }

            foreach (var c in Columns)
            {
                yield return c;
            }
        }
    }

    public class DataColumn : EntityBase
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

        #region 属性 FieldName
        private string _FieldName = string.Empty;
        [XmlAttribute("FieldName"), DefaultValue("")]
        public string FieldName
        {
            get
            {
                return _FieldName;
            }
            set
            {
                if (_FieldName == value)
                {
                    return;
                }
                _FieldName = value;
                RaisePropertyChanged("FieldName");
            }
        }
        #endregion

        #region 属性 Type
        private string _Type = string.Empty;
        [XmlAttribute("Type"), DefaultValue("")]
        public string Type
        {
            get
            {
                return _Type;
            }
            set
            {
                if (_Type == value)
                {
                    return;
                }
                _Type = value;
                RaisePropertyChanged("Type");
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
}
