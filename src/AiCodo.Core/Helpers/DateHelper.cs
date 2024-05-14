// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    public static class DateHelper
    {
        public static DateTime Today = DateTime.Now.Date;

        #region property MinDate
        private static DateTime _MinDate = new DateTime(1900, 1, 1);
        public static DateTime MinDate
        {
            get
            {
                return _MinDate;
            }
        }
        #endregion //property MinDate

        public static bool IsMinDate(this DateTime date)
        {
            return date.Equals(MinDate);
        }

        public static DateTime AddTime(this DateTime date, string time)
        {
            if (string.IsNullOrEmpty(time) || time.Length == 1)
            {
                return date;
            }

            var unit = time[time.Length - 1];
            var add = Convert.ToDouble(time.Substring(0, time.Length - 1));
            switch (unit)
            {
                case 'Y':
                case 'y':
                    return date.AddYears((int)add);
                case 'D':
                case 'd':
                    return date.AddDays(add);
                case 'H':
                case 'h':
                    return date.AddHours(add);
                case 'M':
                    return date.AddMonths((int)add);
                case 'm':
                    return date.AddMinutes(add);
                case 'S':
                case 's':
                    return date.AddSeconds(add);
                default:
                    break;
            }

            return date;
        }
    }
}
