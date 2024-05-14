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
    #region update
    public class UpdateDefinition : Entity
    {
        public UpdateDefinition()
        {

        }

        public UpdateDefinition(object[] nameValues)
        {
            if (nameValues != null && nameValues.Length > 0)
            {
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    this.SetValue(nameValues[i].ToString(), nameValues[i + 1]);
                }
            }
        }

        public UpdateDefinition(IEntity entity)
        {
            if (entity != null)
            {
                var nameValues = entity.GetNameValues();
                for (int i = 0; i < nameValues.Length - 1; i += 2)
                {
                    this.SetValue(nameValues[i].ToString(), nameValues[i + 1]);
                }
            }
        }

        public virtual string CreateUpdateFields(IEntity parameters)
        {
            StringBuilder sb = new StringBuilder();
            var index = 0;
            foreach (var name in this.Keys)
            {
                var pname = $"{name}";
                var pvalue = GetValue(name);
                parameters.SetValue(pname, pvalue);
                if (index == 0)
                {
                    sb.Append($"{name}=@{pname}");
                }
                else
                {
                    sb.Append($",\r\n{name}=@{pname}");
                }
                index++;
            }
            return sb.ToString();
        }

        public virtual string CreateUpdateFields(IEntity parameters, string parameterPrefix, ref int parameterIndex)
        {
            StringBuilder sb = new StringBuilder();
            var index = 0;
            foreach (var name in this.Keys)
            {
                parameterIndex++;
                var pname = $"@{parameterPrefix}{parameterIndex}";
                var pvalue = GetValue(name);
                parameters.SetValue(pname, pvalue);
                if (index == 0)
                {
                    sb.Append($"{name}={pname}");
                }
                else
                {
                    sb.Append($",\r\n{name}={pname}");
                }
                index++;
            }
            return sb.ToString();
        }

        public UpdateDefinition Set(string name, object value)
        {
            this.SetValue(name, value);
            return this;
        }
    }
    #endregion
}
