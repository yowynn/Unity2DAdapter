using System;
using System.Xml;
using System.IO;

namespace Wynnsharp
{
    class XmlUtil
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
            XmlDocument src = new XmlDocument();
            src.Load(srcfile);
            XmlDocument log = new XmlDocument();
            Directory.CreateDirectory(Path.GetDirectoryName(logfile));
            if (!File.Exists(logfile))
            {
                File.Create(logfile).Close();
            }
            if (isAppend)
            {
                try
                {
                    log.Load(logfile);
                }
                catch(Exception)
                {

                }
            }
            Statistics(src, log);
            log.Save(logfile);
        }
    }
}
