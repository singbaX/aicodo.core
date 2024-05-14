using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AiCodo
{
    public class HZConfig : EntityBase
    {
        static object _CreateLock = new object();

        static string _FileName = "Assets\\LangText.xml".FixedAppBasePath();

        static DateTime _LastChanged = DateTime.MinValue;

        static bool _IsLoading = false;

        static bool _IsWaitSaving = false;

        static int _SaveDelaySecond = 5;

        static Dictionary<string, HZItem> _TextItems = new Dictionary<string, HZItem>();

        #region 属性 RemoveChars
        private string _RemoveChars = string.Empty;
        [XmlAttribute("RemoveChars"), DefaultValue("`")]
        public string RemoveChars
        {
            get
            {
                return _RemoveChars;
            }
            set
            {
                if (_RemoveChars == value)
                {
                    return;
                }
                _RemoveChars = value;
                RaisePropertyChanged("RemoveChars");
            }
        }
        #endregion

        #region 属性 BlockChars
        private string _BlockChars = string.Empty;
        [XmlAttribute("BlockChars"), DefaultValue("```")]
        public string BlockChars
        {
            get
            {
                return _BlockChars;
            }
            set
            {
                if (_BlockChars == value)
                {
                    return;
                }
                _BlockChars = value;
                RaisePropertyChanged("BlockChars");
            }
        }
        #endregion

        #region 属性 Items
        private CollectionBase<HZItem> _Items = null;
        [XmlElement("Item", typeof(HZItem))]
        public CollectionBase<HZItem> Items
        {
            get
            {
                if (_Items == null)
                {
                    _Items = new CollectionBase<HZItem>();
                }
                return _Items;
            }
            set
            {
                _Items = value;
                RaisePropertyChanged("Items");
            }
        }
        #endregion

        #region load
        static HZConfig _Current;
        public static HZConfig Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_CreateLock)
                    {
                        if (_Current == null)
                        {
                            _Current = Load();
                            _Current.ResetTexts();
                            _LastChanged = DateTime.Now;
                        }
                    }
                }
                return _Current;
            }
        }

        private static HZConfig Load()
        {
            if (_FileName.IsFileExists())
            {
                try
                {
                    var doc = _FileName.LoadXDoc<HZConfig>();
                    return doc;
                }
                catch (Exception ex)
                {
                    ex.WriteErrorLog();
                    _FileName.CopyFile($"Assets\\LangText_{DateTime.Now.ToString("yyyyMMddHHmmss")}_bak.xml".FixedAppBasePath());
                    return new HZConfig();
                }
            }
            else
            {
                return new HZConfig();
            }
        }

        public void Save()
        {
            this.SaveXDoc(_FileName);
        }

        public void ResetTexts(bool isEN = false)
        {
            lock (_CreateLock)
            {
                _TextItems.Clear();

                foreach (var item in Items)
                {
                    _TextItems[item.Name] = item;
                }
            }
        }
        #endregion

        #region 延迟保存
        private void CheckSaving()
        {
            if (_IsWaitSaving)
            {
                return;
            }
            lock (_CreateLock)
            {
                if (_IsWaitSaving)
                {
                    return;
                }

                _IsWaitSaving = true;
                Threads.StartNew(() =>
                {
                    while (_IsWaitSaving)
                    {
                        var changed = _LastChanged;
                        if (DateTime.Now > changed.AddSeconds(_SaveDelaySecond))
                        {
                            lock (_CreateLock)
                            {
                                Save();
                                if (changed == _LastChanged)
                                {
                                    _IsWaitSaving = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                });
            }
        }
        #endregion

        public void SetText(string name, string langType, string text)
        {
            lock (_TextItems)
            {
                var item = GetItem(name);
                item.SetText(langType, text);
            }
        }

        public string GetText(string sourceText, string langType)
        {
            var sb = new StringBuilder();
            var from = 0;
            for (var i = 0; i < sourceText.Length; i++)
            {
                var c = sourceText[i];
                var isCN = c.IsHZ();
                if (isCN)
                {
                    var n = i;
                    while (true)
                    {
                        n++;
                        if (n < sourceText.Length && sourceText[n].IsHZ())
                        {
                            continue;
                        }
                        break;
                    }
                    var name = sourceText.Substring(i, n - i);
                    sb.Append(GetOrAddItemText(name, langType));
                    i = n - 1;
                    continue;
                }
                if (BlockChars.IsNotEmpty() && BlockChars[0] == c && sourceText.Length > (i + BlockChars.Length * 2))
                {
                    if (sourceText.Substring(i, BlockChars.Length) == BlockChars)
                    {
                        var endIndex = sourceText.IndexOf(BlockChars, i + BlockChars.Length);
                        if (endIndex > 0)
                        {
                            var name = sourceText.Substring(i + BlockChars.Length, (endIndex - i - BlockChars.Length));
                            sb.Append(GetOrAddItemText(name, langType));
                            i = endIndex + BlockChars.Length - 1;
                            continue;
                        }
                    }
                }
                if (RemoveChars.IndexOf(c) < 0)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private HZItem GetItem(string name)
        {
            if (!_TextItems.TryGetValue(name, out var item))
            {
                item = new HZItem(name);
                _TextItems[name] = item;
                Items.Add(item);
            }

            return item;
        }

        private string GetOrAddItemText(string name, string langType)
        {
            lock (_TextItems)
            {
                var item = GetItem(name);
                return item.GetText(langType);
            }
        }
    }

    public class HZItem : EntityBase, IXmlSerializable
    {
        private Dictionary<string, string> _Texts = new Dictionary<string, string>();

        public HZItem()
        {

        }

        public HZItem(string name)
        {
            Name = name;
        }

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

        #region IXmlSerializable
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            while (reader.ReadAttributeValue())
            {
                if (reader.Name == "Name")
                {
                    Name = reader.Value;
                }
                else if (reader.Name.EndsWith("Text"))
                {
                    _Texts[reader.Name] = reader.Value;
                }
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);
            foreach (var item in _Texts)
            {
                writer.WriteAttributeString(item.Key, item.Value);
            }
        }
        #endregion

        public string GetText(string langType)
        {
            string key = GetLangKey(langType);

            lock (_Texts)
            {
                if (_Texts.TryGetValue(key, out var v))
                {
                    return v;
                }
                return Name;
            }
        }

        public void SetText(string langType, string text)
        {
            lock (_Texts)
            {
                string key = GetLangKey(langType);
                if (text.IsNullOrEmpty() || text.Equals(Name))
                {
                    if (_Texts.ContainsKey(key))
                    {
                        _Texts.Remove(key);
                    }
                    return;
                }
                _Texts[key] = text;
            }
        }

        private static string GetLangKey(string langType)
        {
            return $"{langType.ToUpper()}Text";
        }
    }
}
