using System;
using System.Xml;
using System.IO;

namespace Wynnsharp
{
    public class XmlUtil
    {
        public void Statistics(XmlNode src, XmlNode log)
        {
            if (src.Name != log.Name)
            {
                Console.WriteLine($"{src.Name}-{log.Name}--------------");
                throw new Exception();
            }
            var doc = log is XmlDocument ? log as XmlDocument : log.OwnerDocument;
            if (src.Attributes != null)
            {
                {
                    var name = "_count";
                    var la = log.Attributes[name];
                    if (la == null)
                    {
                        la = doc.CreateAttribute(name);
                        la.Value = 1.ToString();
                        log.Attributes.SetNamedItem(la);
                    }
                    else
                    {
                        la.Value = (int.Parse(la.Value) + 1).ToString();
                    }
                }
                foreach (XmlAttribute a in src.Attributes)
                {
                    var name = a.Name;
                    var la = log.Attributes[name];
                    if (la == null)
                    {
                        la = doc.CreateAttribute(name);
                        la.Value = 1.ToString();
                        log.Attributes.SetNamedItem(la);
                    }
                    else
                    {
                        la.Value = (int.Parse(la.Value) + 1).ToString();
                    }
                }
            }

            foreach (XmlNode node in src.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine(node.NodeType);
                    Console.WriteLine(node.Name);
                    continue;
                }
                var name = node.Name;
                XmlNode ln = null;
                foreach (XmlNode lnode in log.ChildNodes)
                {
                    if (lnode.Name == name)
                    {
                        ln = lnode;
                        break;
                    }
                }
                if (ln == null)
                {
                    ln = doc.CreateElement(name);
                    log.AppendChild(ln);
                }
                Statistics(node, ln);
            }
        }

        public void Statistics(string srcfile, string logfile, bool isAppend = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logfile));
            if (!File.Exists(logfile))
            {
                File.Create(logfile).Close();
            }
            XmlDocument src = OpenXml(srcfile) ?? new XmlDocument();
            XmlDocument log = (isAppend ? OpenXml(logfile) : null) ?? new XmlDocument();
            Statistics(src, log);
            log.Save(logfile);
        }

        public XmlNode GetChildNode(XmlNode parent, string name)
        {
            foreach (XmlNode lnode in parent.ChildNodes)
            {
                if (lnode.Name == name)
                {
                    return lnode;
                }
            }
            return null;
        }

        public XmlDocument OpenXml(string filename)
        {
            XmlDocument file = new XmlDocument();
            try
            {
                file.Load(filename);
            }
            catch (Exception)
            {
                return null;
            }
            return file;
        }
    }
}
