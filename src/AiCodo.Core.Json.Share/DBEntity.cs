// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;

    public class DBEntity : Entity
    {
        public DBEntity()
        {
            IsValid = true;
            CreateUser = 0;
            CreateTime = DateTime.Now;
            UpdateUser = 0;
            UpdateTime = DateTime.Now;
        }

        #region IsValid
        public bool IsValid
        {
            get
            {
                return GetFieldValue<bool>("IsValid", true);
            }
            set
            {
                SetFieldValue("IsValid", value);
            }
        }
        #endregion

        #region CreateUser
        public int CreateUser
        {
            get
            {
                return GetFieldValue<int>("CreateUser", 0);
            }
            set
            {
                SetFieldValue("CreateUser", value);
            }
        }
        #endregion

        #region CreateTime
        public DateTime CreateTime
        {
            get
            {
                return GetFieldValue<DateTime>("CreateTime", DateTime.Now);
            }
            set
            {
                SetFieldValue("CreateTime", value);
            }
        }
        #endregion

        #region UpdateUser
        public int UpdateUser
        {
            get
            {
                return GetFieldValue<int>("UpdateUser", 0);
            }
            set
            {
                SetFieldValue("UpdateUser", value);
            }
        }
        #endregion

        #region UpdateTime
        public DateTime UpdateTime
        {
            get
            {
                return GetFieldValue<DateTime>("UpdateTime", DateTime.Now);
            }
            set
            {
                SetFieldValue("UpdateTime", value);
            }
        }
        #endregion

        public virtual void SetCreateUser(int userId)
        {
            CreateUser = userId;
            UpdateUser = userId;
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        public virtual void SetUpdateUser(int userId)
        {
            UpdateUser = userId;
            UpdateTime = DateTime.Now;
        }
    }
}
