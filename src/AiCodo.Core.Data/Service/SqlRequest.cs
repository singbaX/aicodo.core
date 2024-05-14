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
    public class SqlRequest:ISqlRequest
    {
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
            }
        }
        #endregion

        #region 属性 Parameters
        private Dictionary<string, object> _Parameters = null;
        public Dictionary<string, object> Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new Dictionary<string, object>();
                }
                return _Parameters;
            }
            set
            {
                _Parameters = value;
            }
        }
        #endregion

        public virtual object[] GetNameValues()
        {
            return Parameters.ToNameValues();
        }
    }
}
