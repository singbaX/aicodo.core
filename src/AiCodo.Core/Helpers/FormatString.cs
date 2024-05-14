// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using System;
using System.Collections.Generic;
using System.Text;

namespace AiCodo
{
    public static class FormatString
    {
        public static string BindData(this string Format,IDictionary<string, object> data)
        {
            var sb = new StringBuilder();
            var index = 0;
            var endIndex = -1;
            var chr = ' ';
            for (int i = 0; i < Format.Length; i++)
            {
                chr = Format[i];
                if (chr == '{')
                {
                    if (i < Format.Length - 1)
                    {
                        if (Format[i + 1] == '{')
                        {
                            sb.Append('{');
                            i++;
                            continue;
                        }
                        index = i;
                        endIndex = Format.IndexOf('}', index + 1);
                        if (endIndex > index)
                        {
                            var name = Format.Substring(index + 1, endIndex - index - 1).Trim();
                            if (data.TryGetValue(name, out object v))
                            {
                                sb.Append($"{v}");
                            }
                            i = endIndex;
                        }
                        else
                        {
                            sb.Append(Format.Substring(index));
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    sb.Append(chr);
                }
            }

            return sb.ToString();
        }
    }

}
