using System.Collections.Generic;
using System.Xml;

namespace Unity2DAdapter.Util
{
    public static class XmlAnalyzer
    {

        public static bool GetBoolAttribute(this XmlElement e, string attrName, bool defaultValue = false)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeBool(e, attrName, defaultValue);
        }
        public static string GetStringAttribute(this XmlElement e, string attrName, string defaultValue = "")
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeString(e, attrName, defaultValue);
        }
        public static float GetFloatAttribute(this XmlElement e, string attrName, float defaultValue = 0f)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeFloat(e, attrName, defaultValue);
        }
        public static int GetIntegerAttribute(this XmlElement e, string attrName, int defaultValue = 0)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeInt(e, attrName, defaultValue);
        }

        public static XmlElement GetElement(this XmlElement e, string eleName)
        {
            if (e == null) return null;
            AccessStatElement(e, eleName);
            return XmlUtil.GetElement(e, eleName);
        }

        private enum Type
        {
            Attribute,
            Element,
        }
        private static Dictionary<string, Dictionary<string, Type>> ExistsSet = new Dictionary<string, Dictionary<string, Type>>();
        private static Dictionary<string, Dictionary<string, Type>> AccessSet = new Dictionary<string, Dictionary<string, Type>>();
        private static Dictionary<string, Dictionary<string, int>> LogedSet = new Dictionary<string, Dictionary<string, int>>();

        private static void AccessStatAttribute(XmlElement e, string attrName)
        {
            if (!ExistsSet.TryGetValue(e.Name, out var ext))
            {
                ext = new Dictionary<string, Type>();
                ExistsSet.Add(e.Name, ext);
            }
            if (!AccessSet.TryGetValue(e.Name, out var acs))
            {
                acs = new Dictionary<string, Type>();
                AccessSet.Add(e.Name, acs);
            }
            if (e.Attributes != null) foreach (XmlAttribute a in e.Attributes) if (!ext.ContainsKey(a.Name)) ext.Add(a.Name, Type.Attribute);
            if (!acs.ContainsKey(attrName)) acs.Add(attrName, Type.Attribute);
        }

        private static void AccessStatElement(XmlElement e, string eleName)
        {
            if (!ExistsSet.TryGetValue(e.Name, out var ext))
            {
                ext = new Dictionary<string, Type>();
                ExistsSet.Add(e.Name, ext);
            }
            if (!AccessSet.TryGetValue(e.Name, out var acs))
            {
                acs = new Dictionary<string, Type>();
                AccessSet.Add(e.Name, acs);
            }
            foreach (XmlElement a in e) if (!ext.ContainsKey(a.Name)) ext.Add(a.Name, Type.Element);
            if (!acs.ContainsKey(eleName)) acs.Add(eleName, Type.Element);
        }

        public static void LogNonAccessKey(string path, string val)
        {
            if (!LogedSet.TryGetValue(path, out var logd))
            {
                logd = new Dictionary<string, int>();
                LogedSet.Add(path, logd);
            }
            if (!logd.TryGetValue(val, out var count))
            {
                count = 0;
            }
            logd[val] = ++count;
        }

        public static void Flush(string outfile = null)
        {
            if (outfile != null)
            {
                XmlDocument log = XmlUtil.Open();
                var root = XmlUtil.AddElement(log, "Root");
                var elements = XmlUtil.AddElement(root, "Elements");
                foreach (var pair in ExistsSet)
                {
                    var eleName = pair.Key;
                    if (!AccessSet.TryGetValue(eleName, out var acs))
                    {
                        acs = new Dictionary<string, Type>();
                        throw new System.Exception();
                    }
                    var ext = pair.Value;
                    XmlElement e = null;
                    foreach (var pair2 in ext)
                    {
                        var name = pair2.Key;
                        if (!acs.ContainsKey(name))
                        {
                            if (e == null)
                            {
                                e = XmlUtil.AddElement(elements, eleName);
                            }
                            if (pair2.Value == Type.Attribute)
                            {
                                XmlUtil.SetAttribute(e, name, true);
                            }
                            else if (pair2.Value == Type.Element)
                            {
                                XmlUtil.AddElement(e, name);
                            }
                        }
                    }
                }
                var logd = XmlUtil.AddElement(root, "Logged");
                foreach (var pair in LogedSet)
                {
                    var item = XmlUtil.AddElement(logd, "Item");
                    var path = XmlUtil.SetAttribute(item, "_Path", pair.Key);
                    foreach (var pair0 in pair.Value)
                    {
                        XmlUtil.SetAttribute(item, pair0.Key, pair0.Value);
                    }
                }
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outfile));
                log.Save(outfile);
            }
            ExistsSet = new Dictionary<string, Dictionary<string, Type>>();
            AccessSet = new Dictionary<string, Dictionary<string, Type>>();
            LogedSet = new Dictionary<string, Dictionary<string, int>>();
        }
    }
}
