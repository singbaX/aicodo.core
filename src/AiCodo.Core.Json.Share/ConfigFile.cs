// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AiCodo
{
    public class ConfigFile : ConfigFileBase
    {
        private string _FileName = "";

        public static T CreateOrLoad<T>(string fileName) where T : ConfigFile, new()
        {
            T config = default(T);
            var configFile = fileName.FixedAppConfigPath();
            if (configFile.IsFileExists())
            {
                config = configFile.LoadXDoc<T>();
            }
            else
            {
                config = new T();
                config.SaveXDoc(configFile);
            }
            config._FileName = configFile;
            return config;
        }

        public static bool TryLoad<T>(string fileName, out T config) where T : ConfigFile, new()
        {
            config = default(T);
            var configFile = fileName.FixedAppConfigPath();
            if (configFile.IsFileExists())
            {
                config = configFile.LoadXDoc<T>();
                return true;
            }
            return false;
        }

        public override void RaiseConfigChanged()
        {
            base.RaiseConfigChanged();
            if (AutoSave)
            {
                Save();
            }
        }

        public virtual void Save()
        {
            this.SaveXDoc(_FileName);
        }
    }

    public interface INameItem
    {
        string Name { get; }

        string DisplayName { get; }
    }

    public class NameItem : EntityBase, INameItem
    {
        #region 属性 Name
        private string _Name = string.Empty;
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

        #region 属性 DisplayName
        private string _DisplayName = string.Empty;
        public string DisplayName
        {
            get
            {
                return _DisplayName;
            }
            set
            {
                _DisplayName = value;
                RaisePropertyChanged("DisplayName");
            }
        }
        #endregion

        public NameItem() { }

        public NameItem(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }
    }

    public class JsonConfigFile : ConfigFileBase
    {
        private string _FileName = "";

        public static T CreateOrLoad<T>(string fileName) where T : JsonConfigFile, new()
        {
            T config = default(T);
            var configFile = fileName.FixedAppConfigPath();
            if (configFile.IsFileExists())
            {
                config = configFile.ReadFileText().ToJsonObject<T>();
            }
            else
            {
                config = new T();
                config.ToFormatJson().WriteTo(configFile);
            }
            config._FileName = configFile;
            return config;
        }

        public override void RaiseConfigChanged()
        {
            base.RaiseConfigChanged();
            if (AutoSave)
            {
                Save();
            }
        }

        public void Save()
        {
            this.ToFormatJson().WriteTo(_FileName);
        }
    }

    public abstract class ConfigFileBase : EntityBase
    {
        public event EventHandler ConfigChanged;
        private List<string> _IgnoreConfigChanges = new List<string>
        {
            "AutoSave"
        };

        #region 属性 AutoSave
        private bool _AutoSave = false;
        [XmlIgnore, JsonIgnore]
        public bool AutoSave
        {
            get
            {
                return _AutoSave;
            }
            set
            {
                _AutoSave = value;
                RaisePropertyChanged("AutoSave");
            }
        }
        #endregion

        protected override void RaisePropertyChanged(string name)
        {
            base.RaisePropertyChanged(name);
            if (_IgnoreConfigChanges.Contains(name))
            {
                return;
            }
            RaiseConfigChanged();
        }

        public virtual void RaiseConfigChanged()
        {
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void AddToIgnoreConfigChanges(params string[] names)
        {
            names.ForEach(name => _IgnoreConfigChanges.Add(name));
        }
    }

    public abstract class ConfigItemBase : EntityBase
    {
        private List<string> _IgnoreConfigChanges = new List<string>
        {
            "ConfigRoot","Parent"
        };

        ConfigFileBase _Config = null;
        [XmlIgnore, JsonIgnore]
        public ConfigFileBase ConfigRoot
        {
            get
            {
                return _Config;
            }
            set
            {
                SetConfigRoot(value);
            }
        }

        protected void AddToIgnoreConfigChanges(params string[] names)
        {
            names.ForEach(name => _IgnoreConfigChanges.Add(name));
        }

        protected virtual void SetConfigRoot<T>(T config) where T : ConfigFileBase
        {
            _Config = config;
            OnConfigRootChanged();
        }

        protected override void RaisePropertyChanged(string name)
        {
            base.RaisePropertyChanged(name);
            if (_IgnoreConfigChanges.Contains(name))
            {
                return;
            }
            _Config?.RaiseConfigChanged();
        }

        protected virtual void OnConfigRootChanged()
        {

        }
    }
}
