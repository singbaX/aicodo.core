// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo.Data
{
    public class Sort
    {        
        public static Sort SortOfUpdateTime = new Sort { Name = "UpdateTime" };
        public static Sort SortOfUpdateTimeDesc = new Sort { Name = "UpdateTime", IsDesc = true };

        public string Name { get; set; }

        public bool IsDesc { get; set; } = false;

        public Sort() { }

        public Sort(string sort)
        {
            var args = sort.Split(' ');
            if (args.Length == 1)
            {
                Name = args[0];
            }
            else if (args.Length == 2)
            {
                Name = args[0];
                IsDesc = args[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static implicit operator string(Sort sort)
        { return sort.ToString(); }

        public static implicit operator Sort(string sort)
        {
            return new Sort(sort);
        }

        public override string ToString()
        {
            if (Name.IsNullOrEmpty())
            {
                return "";
            }
            return IsDesc ? $"{Name} DESC" : Name;
        }
    }
}
