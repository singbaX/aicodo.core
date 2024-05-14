using AiCodo.Flow.Configs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AiCodo.Flow
{
    public static class FlowActionFactory
    {
        static Dictionary<string, Type> _ActionTypes = new Dictionary<string, Type>();

        public static void RegisterAction<T>(string name) where T : FlowActionBase
        {
            _ActionTypes.Add(name, typeof(T));
        }

        public static IEnumerable<KeyValuePair<string, Type>> GetTypes()
        {
            return _ActionTypes.ToList();
        }
    }

    public class FlowActionCollection : CollectionBase<FlowActionBase>, System.Xml.Serialization.IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
