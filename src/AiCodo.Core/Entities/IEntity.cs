// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    public interface IEntity : INotifyPropertyChanged
    {
        //取得动态扩展属性的值
        object GetValue(string key, object defaultValue = null);
        //设置动态扩展属性的值
        void SetValue(string key, object value);

        void RemoveKey(string key);

        //取所有（动态）属性名称
        IEnumerable<string> GetFieldNames();
        //取所有（动态）属性的名称、值系列
        object[] GetNameValues();
    }
}
