// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AiCodo.Data
{
    #region filter
    public interface ISqlFilter
    {
    }

    public interface ICompareFilter
    {
        string Name { get; }
        object Value { get; }
        LogicCompareType Type { get; set; }
    }

    public interface ILogicFilter
    {
        LogicJoinType Type { get; set; }

        List<ISqlFilter> Items { get; set; }
    }

    public abstract class SqlFilter : ISqlFilter
    {
        public static SqlFilter operator &(SqlFilter l, SqlFilter r)
        {
            return new LogicFilter
            {
                Type = LogicJoinType.And,
                Items = new List<ISqlFilter>
                {
                    l,r
                }
            };
        }
        public static SqlFilter operator |(SqlFilter l, SqlFilter r)
        {
            return new LogicFilter
            {
                Type = LogicJoinType.Or,
                Items = new List<ISqlFilter>
                {
                    l,r
                }
            };
        }

        public static SqlFilter operator !(SqlFilter l)
        {
            return new LogicFilter
            {
                Type = LogicJoinType.Not,
                Items = new List<ISqlFilter>
                {
                    l
                }
            };
        }
    }
    public class CompareFilter : SqlFilter, ICompareFilter
    {
        static Dictionary<LogicCompareType, string> _Types =
            new Dictionary<LogicCompareType, string>
            {
                {LogicCompareType.Eq,"=" },
                {LogicCompareType.Gt,">" },
                {LogicCompareType.Lt,"<" },
                {LogicCompareType.Gte,">=" },
                {LogicCompareType.Lte,"<=" },
                {LogicCompareType.Contains,"like" },
                {LogicCompareType.In,"in" },
            };

        public static Dictionary<LogicCompareType, string> Types { get { return _Types; } }

        public CompareFilter(string name, object value, LogicCompareType type = LogicCompareType.Eq)
        {
            this.Name = name;
            this.Value = value;
            Type = type;
        }

        public string Name { get; }
        public object Value { get; }

        public LogicCompareType Type { get; set; }

        private string GetCompareType()
        {
            if (_Types.TryGetValue(Type, out string t))
            {
                return t;
            }
            return "=";
        }
    }

    public class LogicFilter : SqlFilter, ILogicFilter
    {
        public LogicJoinType Type { get; set; } = LogicJoinType.And;

        public List<ISqlFilter> Items { get; set; } = new List<ISqlFilter>();
    }

    public enum LogicJoinType
    {
        And,
        Or,
        Not
    }

    public enum LogicCompareType
    {
        Eq,
        Gt,
        Lt,
        Gte,
        Lte,
        Contains,
        In
    }

    public class FilterBuilder
    {
        public static SqlFilter Create(FilterItem filter)
        {
            if (filter.Items.Count == 0)
            {
                if (!Enum.TryParse<LogicCompareType>(filter.Type, out LogicCompareType type))
                {
                    type = LogicCompareType.Eq;
                }
                return new CompareFilter(filter.Name, filter.Value, type);
            }
            else
            {
                if (!Enum.TryParse<LogicJoinType>(filter.Type, out LogicJoinType join))
                {
                    join = LogicJoinType.And;
                }
                var subItems = filter.Items.Select(f => Create(f));
                return Join(subItems, join);
            }
        }


        public static SqlFilter Id(string id)
        {
            return new CompareFilter("_id", id, LogicCompareType.Eq);
        }

        public static SqlFilter Eq(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Eq);
        }
        public static SqlFilter Gt(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Gt);
        }

        public static SqlFilter Lt(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Lt);
        }
        public static SqlFilter Gte(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Gte);
        }

        public static SqlFilter Lte(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Lte);
        }

        public static SqlFilter Contains(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.Contains);
        }

        public static SqlFilter In(string name, object value)
        {
            return new CompareFilter(name, value, LogicCompareType.In);
        }

        public static SqlFilter Join(IEnumerable<SqlFilter> filters, LogicJoinType type = LogicJoinType.And)
        {
            return new LogicFilter
            {
                Items = filters.ToList<ISqlFilter>(),
                Type = type
            };
        }

        public static SqlFilter And(IEnumerable<SqlFilter> filters)
        {
            return new LogicFilter
            {
                Items = filters.ToList<ISqlFilter>(),
                Type = LogicJoinType.And
            };
        }

        //public static SqlFilter Or(IEnumerable<SqlFilter> filters)
        //{
        //    return new LogicFilter
        //    {
        //        Items = filters.ToList<ISqlFilter>(),
        //        Type = LogicJoinType.Or
        //    };
        //}

        public static SqlFilter Or(params ISqlFilter[] filters)
        {
            return new LogicFilter
            {
                Items = filters.ToList<ISqlFilter>(),
                Type = LogicJoinType.Or
            };
        }

        public static SqlFilter Not(SqlFilter filter)
        {
            return new LogicFilter
            {
                Items = new List<ISqlFilter>
               {
                   filter
               },
                Type = LogicJoinType.Not
            };
        }
    }
    #endregion
}
