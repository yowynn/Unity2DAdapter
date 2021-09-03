using System;
using System.Xml;
using System.Collections.Generic;

namespace Cocos2Unity
{
    public class CsdSize
    {
        public float X;
        public float Y;
        public CsdSize(XmlElement e)
        {
            X = float.Parse(e?.Attributes["X"]?.Value ?? "0");
            Y = float.Parse(e?.Attributes["Y"]?.Value ?? "0");
        }
    }

    public class CsdScale
    {
        public float X;
        public float Y;
        public CsdScale(XmlElement e)
        {
            X = float.Parse(e?.Attributes["ScaleX"]?.Value ?? "1");
            Y = float.Parse(e?.Attributes["ScaleY"]?.Value ?? "1");
        }
    }

    public class CsdFile
    {
        public string Type;
        public string Path;
        public string Plist;
        public CsdFile(XmlElement e)
        {
            Type = e.Attributes["Type"].Value;
            Path = e.Attributes["Path"].Value;
            Plist = e.Attributes["Plist"].Value;
        }
    }

    public class CsdNode
    {
        public string Name;
        public CsdSize Position;
        public CsdSize Size;
        public CsdScale Scale;
        public CsdFile Image;
        public List<CsdNode> Children;
        public CsdNode(XmlElement e)
        {
            Name = e.Attributes["Name"].Value;
            Position = new CsdSize(e["Position"]);
            Size = new CsdSize(e["Size"]);
            if (e["FileData"] != null)
            {
                Image = new CsdFile(e["FileData"]);
            }
            if (e["Children"] != null)
            {
                Children = new List<CsdNode>();
                foreach (XmlElement child in e["Children"])
                {
                    Children.Add(new CsdNode(child));
                }
            }
        }
    }
    public class CsdParser
    {
        public String Version;
        public String Name;
        public CsdNode Node;
        public CsdParser(XmlDocument doc)
        {
            var root = doc["GameProjectFile"];

            var prop = root["PropertyGroup"];
            Name = prop.Attributes["Name"].Value;
            Version = prop.Attributes["Version"].Value;

            var content = root["Content"]?["Content"];
            if (content["ObjectData"] != null)
            {
                Node = new CsdNode(content["ObjectData"]);
            }
        }
    }
}
