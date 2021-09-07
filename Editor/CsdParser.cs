using System;
using System.Xml;
using System.Collections.Generic;

namespace Cocos2Unity
{
    public class ParseMeta
    {
        public static bool GetBoolAttribute(XmlElement e, string attrName, bool defaultValue = false)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = e.Attributes?[attrName]?.Value;
            if (val == "True") return true;
            if (val == "False") return false;
            return defaultValue;
        }
        public static string GetStringAttribute(XmlElement e, string attrName, string defaultValue = "")
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = e.Attributes?[attrName]?.Value;
            if (val != null) return val;
            return defaultValue;
        }
        public static float GetFloatAttribute(XmlElement e, string attrName, float defaultValue = 0f)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = e.Attributes?[attrName]?.Value;
            if (val != null) return float.Parse(val);
            return defaultValue;
        }
        public static int GetIntegerAttribute(XmlElement e, string attrName, int defaultValue = 0)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = e.Attributes?[attrName]?.Value;
            if (val != null) return int.Parse(val);
            return defaultValue;
        }

        public static XmlElement GetElement(XmlElement e, string eleName)
        {
            if (e == null) return null;
            AccessStatElement(e, eleName);
            return e[eleName];
        }

        private enum Type{
            Attribute,
            Element,
        }
        private static Dictionary<string, Dictionary<string, Type>> ExistsSet = new Dictionary<string, Dictionary<string, Type>>();
        private static Dictionary<string, Dictionary<string, Type>> AccessSet = new Dictionary<string, Dictionary<string, Type>>();

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

        public static void SwapAccessLog(string outfile = null)
        {
            if (outfile != null)
            {
                XmlDocument log = new XmlDocument();
                var root = log.CreateElement("Elements");
                log.AppendChild(root);
                foreach (var pair in ExistsSet)
                {
                    var eleName = pair.Key;
                    if (!AccessSet.TryGetValue(eleName, out var acs))
                    {
                        acs = new Dictionary<string, Type>();
                        throw new Exception();
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
                                e = log.CreateElement(eleName);
                                root.AppendChild(e);
                            }
                            if (pair2.Value == Type.Attribute)
                            {
                                var la = log.CreateAttribute(name);
                                la.Value = "True";
                                e.Attributes.SetNamedItem(la);
                            }
                            else if (pair2.Value == Type.Element)
                            {
                                var la = log.CreateElement(name);
                                e.AppendChild(la);
                            }
                        }
                    }
                }
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outfile));
                log.Save(outfile);
            }
            ExistsSet = new Dictionary<string, Dictionary<string, Type>>();
            AccessSet = new Dictionary<string, Dictionary<string, Type>>();
        }
    }

    public class CsdSize : ParseMeta
    {
        public float X;
        public float Y;
        public float Z;
        public CsdSize(XmlElement e)
        {
            X = GetFloatAttribute(e, "X", 0);
            Y = GetFloatAttribute(e, "Y", 0);
            Z = GetFloatAttribute(e, "Z", 0);
        }
    }

    public class CsdScale : ParseMeta
    {
        public float X;
        public float Y;
        public float Z;
        public CsdScale(XmlElement e, float defaultValue = 0)
        {
            X = GetFloatAttribute(e, "ScaleX", defaultValue);
            Y = GetFloatAttribute(e, "ScaleY", defaultValue);
            Z = GetFloatAttribute(e, "ScaleZ", defaultValue);
        }
    }

    public class CsdColor : ParseMeta
    {
        public float R;
        public float G;
        public float B;
        public float A;
        public CsdColor(XmlElement e)
        {
            R = GetIntegerAttribute(e, "R", 255) / 255f;
            G = GetIntegerAttribute(e, "G", 255) / 255f;
            B = GetIntegerAttribute(e, "B", 255) / 255f;
            A = GetIntegerAttribute(e, "A", 255) / 255f;
        }
    }

    public class CsdColorGradient : ParseMeta
    {
        public enum ColorMode
        {
            None = 0,
            Color = 1,
            Gradient = 2,
        }
        public ColorMode Mode;
        public CsdColor FromColor;
        public CsdColor ToColor;
        public CsdScale Direction;

        public static CsdColorGradient NewNone()
        {
            var cdg = new CsdColorGradient();
            cdg.Mode = ColorMode.None;
            return cdg;
        }
        public static CsdColorGradient NewColor(CsdColor color)
        {
            var cdg = new CsdColorGradient();
            cdg.Mode = ColorMode.Color;
            cdg.FromColor = color;
            return cdg;
        }
        public static CsdColorGradient NewGradient(CsdColor color1, CsdColor color2, CsdScale dir)
        {
            var cdg = new CsdColorGradient();
            cdg.Mode = ColorMode.Gradient;
            cdg.FromColor = color1;
            cdg.ToColor = color2;
            cdg.Direction = dir;
            return cdg;
        }
    }

    public class CsdFile : ParseMeta
    {
        public string Type;
        public string Path;
        public string Plist;
        public CsdFile(XmlElement e)
        {
            Type = GetStringAttribute(e, "Type", null);
            Path = GetStringAttribute(e, "Path", null);
            Plist = GetStringAttribute(e, "Plist", null);
        }
    }

    public class CsdNode : ParseMeta
    {
        public string Name;
        public bool isActive;
        public bool isInteractive;
        public CsdSize Position;
        public CsdSize Rotation;
        public CsdScale Scale;
        public CsdSize Size;
        public CsdScale Pivot;
        public CsdScale Anchor;
        public CsdFile Image;
        public CsdColor Color;
        public CsdColorGradient BackgroundColor;
        public List<CsdNode> Children;
        public CsdNode(XmlElement e, CsdNode parent = null)
        {
            Name = GetStringAttribute(e, "Name");
            isActive = GetBoolAttribute(e, "VisibleForFrame", true);
            isInteractive = GetBoolAttribute(e, "TouchEnable", false);
            Position = new CsdSize(GetElement(e, "Position"));
            Rotation = GenRotation(e);
            Scale = new CsdScale(GetElement(e, "Scale"), 1f);
            Size = new CsdSize(GetElement(e, "Size"));
            Pivot = new CsdScale(GetElement(e, "AnchorPoint"), 0f);
            Anchor = GenAnchors(e, parent);
            if (GetElement(e, "FileData") != null)
            {
                Image = new CsdFile(GetElement(e, "FileData"));
            }
            if (GetElement(e, "CColor") != null)
            {
                Color = new CsdColor(GetElement(e, "CColor"));
                var Alpha = GetIntegerAttribute(e, "Alpha", 255) / 255f;
                Color.A = Alpha;
            }
            BackgroundColor = GenBackgroundColor(e);


            if (GetElement(e, "Children") != null)
            {
                Children = new List<CsdNode>();
                foreach (XmlElement child in GetElement(e, "Children"))
                {
                    Children.Add(new CsdNode(child, this));
                }
            }
        }

        public static CsdColorGradient GenBackgroundColor(XmlElement e)
        {
            CsdColorGradient backgroundColor = null;
            var BackgroundType = GetIntegerAttribute(e, "ComboBoxIndex", 0);
            var Alpha = GetIntegerAttribute(e, "BackColorAlpha", 255) / 255f;
            var color = new CsdColor(GetElement(e, "SingleColor"));
            color.A = Alpha;
            var color1 = new CsdColor(GetElement(e, "FirstColor"));
            color1.A = Alpha;
            var color2 = new CsdColor(GetElement(e, "EndColor"));
            color2.A = Alpha;
            var dir = new CsdScale(GetElement(e, "ColorVector"));
            var dir_ignored = GetFloatAttribute(e, "ColorAngle", 90f);  //same mean as ColorVector, but in degrees
            switch (BackgroundType)
            {
                case 0: // None
                    // backgroundColor = CsdColorGradient.NewNone();
                    break;
                case 1: // Color
                    backgroundColor = CsdColorGradient.NewColor(color);
                    break;
                case 2: // Gradient
                    backgroundColor = CsdColorGradient.NewGradient(color1, color2, dir);
                    break;
            }
            return backgroundColor;
        }

        public static CsdScale GenAnchors(XmlElement e, CsdNode parent)
        {
            var Anchor = new CsdScale(null);
            var HorizontalEdge = GetStringAttribute(e, "HorizontalEdge", "LeftEdge");
            var VerticalEdge = GetStringAttribute(e, "VerticalEdge", "BottomEdge");
            var LeftMargin = GetFloatAttribute(e, "LeftMargin", 0f);
            var RightMargin = GetFloatAttribute(e, "RightMargin", 0f);
            var TopMargin = GetFloatAttribute(e, "TopMargin", 0f);
            var BottomMargin = GetFloatAttribute(e, "BottomMargin", 0f);
            var parentSize = parent?.Size ?? new CsdSize(null);
            var Pivot = new CsdScale(GetElement(e, "AnchorPoint"), 0f);
            var Size = new CsdSize(GetElement(e, "Size"));
            switch (HorizontalEdge)
            {
                case "LeftEdge":
                    Anchor.X = 0f;
                    break;
                case "RightEdge":
                    Anchor.X = 1f;
                    break;
                case "BothEdge":
                    Anchor.X = (Size.X * Pivot.X + LeftMargin) / (Size.X + LeftMargin + RightMargin);
                    break;
            }
            switch (VerticalEdge)
            {
                case "BottomEdge":
                    Anchor.Y = 0f;
                    break;
                case "TopEdge":
                    Anchor.Y = 1f;
                    break;
                case "BothEdge":
                    Anchor.Y = (Size.Y * Pivot.Y + BottomMargin) / (Size.Y + BottomMargin + TopMargin);
                    // TODO
                    break;
            }
            return Anchor;
        }

        public static CsdSize GenRotation(XmlElement e)
        {
            var Rotation = new CsdSize(null);
            var RotationZ = -GetFloatAttribute(e, "Rotation", 0f);
            var RotationSkewX = -GetFloatAttribute(e, "RotationSkewX", 0f);  // TODO
            var RotationSkewY = -GetFloatAttribute(e, "RotationSkewY", 0f);  // TODO
            Rotation.Z = RotationZ;
            return Rotation;
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
