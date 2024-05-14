// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    public class SqlItemsSource : EntityBase
    {
        public event EventHandler ItemsChanged;

        public SqlItemsSource()
        {
            
        }

        public SqlItemsSource Start()
        {
            IsStart = true;
            return this;
        }

        #region IsStart
        private bool _IsStart = false;
        /// <summary>
        /// 
        /// </summary>
        public bool IsStart
        {
            get
            {
                return _IsStart;
            }
            set
            {
                _IsStart = value; 
                RaisePropertyChanged("IsStart");
                ResetItems();
            }
        }
        #endregion IsStart

        #region 属性 SqlName
        private string _SqlName = string.Empty;
        public string SqlName
        {
            get
            {
                return _SqlName;
            }
            set
            {
                _SqlName = value;
                RaisePropertyChanged("SqlName");
                ResetItems();
            }
        }
        #endregion

        #region 属性 Parameters
        private DynamicEntity _Parameters = null;
        public DynamicEntity Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new DynamicEntity();
                }
                return _Parameters;
            }
            set
            {
                _Parameters = value;
                RaisePropertyChanged("Parameters");
                ResetItems();
            }
        }
        #endregion

        #region 属性 DataContext
        private object _DataContext = null;
        public object DataContext
        {
            get
            {
                return _DataContext;
            }
            set
            {
                if (_DataContext != null)
                {
                    if (_DataContext is INotifyPropertyChanged sourceContext)
                    {
                        sourceContext.PropertyChanged -= SourceContext_PropertyChanged;
                    }
                }

                _DataContext = value;
                RaisePropertyChanged("DataContext");
                if (_DataContext != null)
                {
                    if (_DataContext is INotifyPropertyChanged sourceContext)
                    {
                        sourceContext.PropertyChanged += SourceContext_PropertyChanged;
                    }
                    ResetParametersByDataContext();
                }
            }
        }

        private void ResetParametersByDataContext()
        {
            if (DataContext == null || DataContextMapping == null || DataContextMapping.Count == 0)
            {
                return;
            }
            if (DataContext is IEntity entity)
            {
                foreach (var key in DataContextMapping.Keys)
                {
                    var value = entity.GetValue(key, "");
                    var name = DataContextMapping.GetString(key, "");
                    if (name.IsNotEmpty())
                    {
                        Parameters.SetValue(name, value);
                    }
                }
            }
            else
            {
                foreach (var key in DataContextMapping.Keys)
                {
                    var value = DataContext.GetPathValue(key);
                    var name = DataContextMapping.GetString(key, "");
                    if (name.IsNotEmpty())
                    {
                        Parameters.SetValue(name, value);
                    }
                }
            }
            ResetItems();
        }

        private void SourceContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext == null || DataContextMapping == null || DataContextMapping.Count == 0)
            {
                return;
            }
            if (DataContextMapping.ContainsKey(e.PropertyName))
            {
                ResetParametersByDataContext();
            }
        }
        #endregion

        #region 属性 DataContextMapping
        private DynamicEntity _DataContextMapping = "";
        public DynamicEntity DataContextMapping
        {
            get
            {
                return _DataContextMapping;
            }
            set
            {
                _DataContextMapping = value;
                RaisePropertyChanged("DataContextMapping");
                ResetParametersByDataContext();
            }
        }
        #endregion 

        #region 属性 EmptyItem
        private DynamicEntity _EmptyItem = null;
        public DynamicEntity EmptyItem
        {
            get
            {
                return _EmptyItem;
            }
            set
            {
                _EmptyItem = value;
                RaisePropertyChanged("EmptyItem");
            }
        }
        #endregion

        #region 属性 Items
        private IEnumerable _Items = null;
        public IEnumerable Items
        {
            get
            {
                return _Items;
            }
            private set
            {
                _Items = value;
                RaisePropertyChanged("Items");
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private IEnumerable<DynamicEntity> GetItems()
        {
            if (SqlName.IsNullOrEmpty())
            {
                this.Log($"没有指定SQL语句");
                yield break;
            }
            
            var data = SqlService.ExecuteSql(SqlName, Parameters.ToNameValues());
            if (data == null)
            {
                this.Log($"[{SqlName}]查询数据为空"); 
                yield break;
            }
            if (EmptyItem != null)
            {
                yield return EmptyItem;
            }
            if (data is IEnumerable list)
            {
                var count = 0;
                foreach (DynamicEntity item in list)
                {
                    yield return item;
                    count++;
                }
                this.Log($"[{SqlName}]查询数据[{count}]");
            }
            yield break;
        }
        public void ResetItems()
        {
            if (!IsStart)
            {
                return;
            }
            Items = GetItems().ToList();
        }
        #endregion
    }
}
