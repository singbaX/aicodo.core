// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;

    public delegate void LogDelegate(object sender, string msg, Category category = Category.Info);


    public enum Priority
    {
        /// <summary>
        /// 没有指定
        /// </summary>
        None = 0,

        /// <summary>
        /// 高级
        /// </summary>
        High = 1,

        /// <summary>
        /// 中级
        /// </summary>
        Medium = 2,

        /// <summary>
        /// 低级
        /// </summary>
        Low = 3,
    }

    /// <summary>
    /// 错误类别
    /// </summary>
    [Flags]
    public enum Category
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug = 1,

        /// <summary>
        /// 异常
        /// </summary>
        Exception = 2,

        /// <summary>
        /// 信息
        /// </summary>
        Info = 4,

        /// <summary>
        /// 警告
        /// </summary>
        Warn = 8,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal = 16,
        /// <summary>
        /// 所有
        /// </summary>
        All = 31
    }

}
