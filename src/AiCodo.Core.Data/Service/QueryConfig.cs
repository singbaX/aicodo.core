using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AiCodo.Data
{
    public class QueryResult : EntityBase
    {
        #region 属性 TotalCount
        private int _TotalCount = 0;
        [JsonProperty("total", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public int TotalCount
        {
            get
            {
                return _TotalCount;
            }
            set
            {
                _TotalCount = value;
                RaisePropertyChanged("TotalCount");
            }
        }
        #endregion

        #region 属性 PageIndex
        private int _PageIndex = 0;
        [JsonProperty("page", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public int PageIndex
        {
            get
            {
                return _PageIndex;
            }
            set
            {
                _PageIndex = value;
                RaisePropertyChanged("PageIndex");
            }
        }
        #endregion

        #region 属性 PageSize
        private int _PageSize = 0;
        [JsonProperty("size", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                _PageSize = value;
                RaisePropertyChanged("PageSize");
            }
        }
        #endregion

        #region 属性 Items
        private List<DynamicEntity> _Items = null;
        [JsonProperty("items")]
        public List<DynamicEntity> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new List<DynamicEntity>();
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

    public class QueryConfig : EntityBase
    {
        #region 属性 PageIndex
        private int _PageIndex = 0;
        [JsonProperty("page", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public int PageIndex
        {
            get
            {
                return _PageIndex;
            }
            set
            {
                _PageIndex = value;
                RaisePropertyChanged("PageIndex");
            }
        }
        #endregion

        #region 属性 PageSize
        private int _PageSize = 0;
        [JsonProperty("size", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(0)]
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                _PageSize = value;
                RaisePropertyChanged("PageSize");
            }
        }
        #endregion

        #region 属性 Filter
        private QueryFilter _Filter = null;
        [JsonProperty("filter")]
        public QueryFilter Filter
        {
            get
            {
                if (_Filter == null)
                {
                    _Filter = new QueryFilter();
                }
                return _Filter;
            }
            set
            {
                _Filter = value;
                RaisePropertyChanged("Filter");
            }
        }
        #endregion

        #region 属性 OrderBy
        private string _OrderBy = string.Empty;
        [JsonProperty("orderby")]
        public string OrderBy
        {
            get
            {
                return _OrderBy;
            }
            set
            {
                _OrderBy = value;
                RaisePropertyChanged(() => OrderBy);
            }
        }
        #endregion
    }

    public class QueryFilter : EntityBase
    {
        Dictionary<string, LogicCompareType> _CompareTypes = new Dictionary<string, LogicCompareType>()
        {
            {"eq", LogicCompareType.Eq },
            {"gt",LogicCompareType.Gt },
            {"lt",LogicCompareType.Lt },
            {"gte",LogicCompareType.Gte },
            {"lte",LogicCompareType.Lte },
            {"contains",LogicCompareType.Contains },
            {"in",LogicCompareType.In },
        };
        Dictionary<string, LogicJoinType> _JoinTypes = new Dictionary<string, LogicJoinType>()
        {
            {"and", LogicJoinType.And } ,
            {"or" ,LogicJoinType.Or}
        };

        #region 属性 Name
        private string _Name = string.Empty;
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
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
        private string _Type = string.Empty;
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
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
        [JsonProperty("value", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
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
        private CollectionBase<QueryFilter> _Items = null;
        [JsonProperty("items")]
        public CollectionBase<QueryFilter> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new CollectionBase<QueryFilter>();
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

        public SqlFilter GetFilter()
        {
            if (Items.Count == 0)
            {
                if (_CompareTypes.TryGetValue(Type.ToLower(), out var type))
                {
                    return new CompareFilter(Name, Value, type);
                }
                return null;
            }
            switch (Type.ToLower())
            {
                case "and":
                    return FilterBuilder.And(Items.Select(f => f.GetFilter()));
                case "or":
                    return FilterBuilder.Or(Items.Select(f => f.GetFilter()).ToArray());
                default:
                    break;
            }
            return null;
        }
    }
}
