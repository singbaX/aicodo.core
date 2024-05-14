using System.Collections.Generic;

namespace AiCodo
{
    public static class AppSetting
    {
        static Dictionary<string, object> _SettingValues
            = new Dictionary<string, object>();

        #region 属性 AppSettingFile
        const string _AppSettingFile = "app.json";
        private static string _SettingFile = string.Empty;
        public static string AppSettingFile
        {
            get
            {
                if (string.IsNullOrEmpty(_SettingFile))
                {
                    _SettingFile = GetSettingFile(_AppSettingFile);
                }
                return _SettingFile;
            }
            set
            {
                _SettingFile = value;
            }
        }

        public static string GetSettingFile(string fileName)
        {
            if (fileName.Contains(":"))
            {
                return fileName;
            }
            var localFile = fileName.FixedAppBasePath();
            if (System.IO.File.Exists(localFile))
            {
                return localFile;
            }

            return localFile;
        }

        #endregion

        #region ApplicationSetting        
        static DynamicEntity _ApplicationSetting = AppSettingFile.ReadJsonSetting();

        public static DynamicEntity ApplicationSetting
        {
            get
            {
                return _ApplicationSetting;
            }
        }
        #endregion

        #region Appsetting 
        public static T GetAppSetting<T>(this string sectionName, T defaultValue)
        {
            var setting = _SettingValues.ContainsKey(sectionName) ?
                _SettingValues[sectionName] :
                ApplicationSetting.GetValue(sectionName, null);
            if (setting == null)
            {
                return defaultValue;
            }
            return setting.ToJson().ToJsonObject<T>();
        }

        public static void SetAppSetting<T>(this string sectionName, T value)
        {
            _SettingValues[sectionName] = value;
            ApplicationSetting.SetValue(sectionName, value);

            string localFile = _AppSettingFile.FixedAppDataPath(); //Path.Combine(LocalDataFolder, _AppSettingFile);
            ApplicationSetting.ToJson().WriteTo(localFile);
        }

        public static DynamicEntity ReadJsonSetting(this string settingName)
        {
            var file = GetSettingFile(settingName);
            if (System.IO.File.Exists(file))
            {
                var content = file.ReadFileText();
                DynamicEntity setting = content;
                return setting;
            }
            return new DynamicEntity();
        }
        #endregion
    }
}
