// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace AiCodo.Flow.Configs
{
    [XmlRoot("ServiceIndex")]
    public class ServiceIndex : ConfigFile
    {
        #region master items
        Dictionary<string, MasterFileItem> _MasterIDItems = new Dictionary<string, MasterFileItem>();
        private void ResetMasterIDItems()
        {
            lock (_MasterIDItems)
            {
                _MasterIDItems.Clear();
                MasterItems.ToList().ForEach(f => _MasterIDItems[f.ID] = f);
            }
        }

        private void AddMasterItem(MasterFileItem item)
        {
            lock (_MasterIDItems)
            {
                _MasterIDItems[item.ID] = item;
            }
        }

        private bool RemoveMasterItem(string id)
        {
            lock (_MasterIDItems)
            {
                return _MasterIDItems.Remove(id);
            }
        }

        public bool TryGetMasterItem(string id, out MasterFileItem item)
        {
            lock (_MasterIDItems)
            {
                return _MasterIDItems.TryGetValue(id, out item);
            }
        }
        #endregion

        #region NameItems 
        Dictionary<string, ServiceItemBase> _NameItems = new Dictionary<string, ServiceItemBase>();
        private void ResetNameItems()
        {
            lock (_NameItems)
            {
                _NameItems.Clear();
                Items.ToList().ForEach(f => _NameItems[f.Name.ToLower()] = f);
            }
        }

        private void AddNameItem(ServiceItemBase item)
        {
            lock (_NameItems)
            {
                _NameItems[item.Name.ToLower()] = item;
            }
        }

        private bool RemoveNameItem(string name)
        {
            lock (_NameItems)
            {
                return _NameItems.Remove(name.ToLower());
            }
        }

        public bool TryGetItem(string name, out ServiceItemBase item)
        {
            lock (_NameItems)
            {
                return _NameItems.TryGetValue(name.ToLower(), out item);
            }
        }
        #endregion

        #region 属性 Current
        private static ServiceIndex _Current = null;
        private static object _LoadLock = new object();
        public static ServiceIndex Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_LoadLock)
                    {
                        if (_Current == null)
                        {
                            _Current = CreateOrLoad<ServiceIndex>("ServiceIndex.xml");
                        }
                    }
                }
                return _Current;
            }
        }
        #endregion

        #region 属性 MasterItems
        private CollectionBase<MasterFileItem> _MasterItems = null;
        [XmlElement("Master", typeof(MasterFileItem))]
        public CollectionBase<MasterFileItem> MasterItems
        {
            get
            {
                if (_MasterItems == null)
                {
                    _MasterItems = new CollectionBase<MasterFileItem>();
                    _MasterItems.CollectionChanged += MasterItems_CollectionChanged;
                }
                return _MasterItems;
            }
            set
            {
                if (_MasterItems != null)
                {
                    _MasterItems.CollectionChanged -= MasterItems_CollectionChanged;
                    OnMasterItemsRemoved(_MasterItems);
                }
                _MasterItems = value;
                RaisePropertyChanged("MasterItems");
                if (_MasterItems != null)
                {
                    _MasterItems.CollectionChanged += MasterItems_CollectionChanged;
                    OnMasterItemsAdded(_MasterItems);
                }
            }
        }

        private void MasterItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnMasterItemsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnMasterItemsRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnMasterItemsAdded(IList newItems)
        {
            foreach (MasterFileItem item in newItems)
            {
                item.ConfigRoot = this;
            }
        }

        protected virtual void OnMasterItemsRemoved(IList oldItems)
        {
            foreach (MasterFileItem item in oldItems)
            {
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 Items
        private CollectionBase<ServiceItem> _Items = null;
        [XmlElement("Item", typeof(ServiceItem))]
        public CollectionBase<ServiceItem> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new CollectionBase<ServiceItem>();
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
                ResetNameItems();
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
            foreach (ServiceItem item in newItems)
            {
                item.ConfigRoot = this;
                AddNameItem(item);
            }
        }

        protected virtual void OnItemsRemoved(IList oldItems)
        {
            foreach (ServiceItem item in oldItems)
            {
                item.ConfigRoot = null;
                RemoveNameItem(item.Name);
            }
        }
        #endregion

        public string GetNewRefID()
        {
            var id = 0;
            foreach (var rid in MasterItems.Select(r => r.ID).Where(r => r.StartsWith("Ref")).Select(s => s.Substring(3)))
            {
                var mid = rid.ToInt32();
                if (mid > id)
                {
                    id = mid;
                }
            }
            id++;
            return $"Ref{id.ToString("d4")}";
        }

        public string GetNewID()
        {
            var id = 0;
            foreach (var rid in Items.Select(r => r.ID).Where(r => r.StartsWith("F")).Select(s => s.Substring(1)))
            {
                var mid = rid.ToInt32();
                if (mid > id)
                {
                    id = mid;
                }
            }
            id++;
            return $"F{id.ToString("d4")}";
        }
    }

    public class FunctionFlowFileItem : FunctionFlowFileItemBase
    {
    }

    public class MasterFileItem : FunctionFlowFileItemBase
    {
        #region 属性 FlowConfig
        private MasterFlowConfig _FlowConfig = null;
        [XmlIgnore, JsonIgnore]
        public MasterFlowConfig FlowConfig
        {
            get
            {
                if (_FlowConfig == null)
                {
                    _FlowConfig = MasterFlowConfig.Load(GetFileID());
                }
                return _FlowConfig;
            }
        }
        #endregion
    }

    public class FunctionFlowFileItemBase : ServiceItemBase
    {
        public string GetFileID()
        {
            return string.IsNullOrEmpty(ServiceName) ? ID : ServiceName;
        }
    }

    public class FunctionFlowSqlItem : ServiceItemBase
    {
        #region 属性 SqlName
        private string _SqlName = string.Empty;
        [XmlAttribute("SqlName"), DefaultValue("")]
        public string SqlName
        {
            get
            {
                return _SqlName;
            }
            set
            {
                if (_SqlName == value)
                {
                    return;
                }
                _SqlName = value;
                RaisePropertyChanged("SqlName");
            }
        }
        #endregion
    }

    public class ServiceItem : ServiceItemBase
    {
    }

    public class ServiceItemBase : ConfigItemBase
    {
        #region 属性 ID
        private string _ID = string.Empty;
        [XmlAttribute("ID"), DefaultValue("")]
        public string ID
        {
            get
            {
                return _ID;
            }
            set
            {
                if (_ID == value)
                {
                    return;
                }
                _ID = value;
                RaisePropertyChanged("ID");
            }
        }
        #endregion

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

        #region 属性 ServiceName
        private string _ServiceName = string.Empty;
        [XmlAttribute("ServiceName"), DefaultValue("")]
        public string ServiceName
        {
            get
            {
                return _ServiceName;
            }
            set
            {
                if (_ServiceName == value)
                {
                    return;
                }
                _ServiceName = value;
                RaisePropertyChanged("ServiceName");
            }
        }
        #endregion

        #region 属性 ServiceArgs
        private string _ServiceArgs = string.Empty;
        [XmlAttribute("ServiceArgs"), DefaultValue("")]
        public string ServiceArgs
        {
            get
            {
                return _ServiceArgs;
            }
            set
            {
                if (_ServiceArgs == value)
                {
                    return;
                }
                _ServiceArgs = value;
                RaisePropertyChanged("ServiceArgs");
            }
        }
        #endregion

        #region 属性 PageID
        private string _PageID = string.Empty;
        [XmlAttribute("PageID"), DefaultValue("")]
        public string PageID
        {
            get
            {
                return _PageID;
            }
            set
            {
                _PageID = value;
                RaisePropertyChanged(() => PageID);
            }
        }
        #endregion

        #region 属性 AuthValue
        private int _AuthValue = 0;
        [XmlAttribute("AuthValue"), DefaultValue(0)]
        public int AuthValue
        {
            get
            {
                return _AuthValue;
            }
            set
            {
                if (_AuthValue == value)
                {
                    return;
                }
                _AuthValue = value;
                RaisePropertyChanged("AuthValue");
            }
        }
        #endregion

        #region 属性 ResultType //返回值类型
        private ServiceResultType _ResultType = ServiceResultType.Default;
        [XmlAttribute("ResultType"), DefaultValue(typeof(ServiceResultType), "Default")]
        public ServiceResultType ResultType
        {
            get
            {
                return _ResultType;
            }
            set
            {
                if (_ResultType == value)
                {
                    return;
                }
                _ResultType = value;
                RaisePropertyChanged("ResultType");
            }
        }
        #endregion
    }

    public enum ServiceResultType
    {
        Default,
        FileStream
    }
}
