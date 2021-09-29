using System;
using System.Xml;
using System.Collections.Generic;
using Wynncs.Entry;
using Wynncs.Util;

namespace Cocos2Unity
{
    public interface ICsdParse<T> where T : new()
    {
        T Parse(XmlElement e);
    }
    public abstract class CsdType
    {
        public static bool GetBoolAttribute(XmlElement e, string attrName, bool defaultValue = false)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeBool(e, attrName, defaultValue);
        }
        public static string GetStringAttribute(XmlElement e, string attrName, string defaultValue = "")
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeString(e, attrName, defaultValue);
        }
        public static float GetFloatAttribute(XmlElement e, string attrName, float defaultValue = 0f)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeFloat(e, attrName, defaultValue);
        }
        public static int GetIntegerAttribute(XmlElement e, string attrName, int defaultValue = 0)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            return XmlUtil.GetAttributeInt(e, attrName, defaultValue);
        }

        public static XmlElement GetElement(XmlElement e, string eleName)
        {
            if (e == null) return null;
            AccessStatElement(e, eleName);
            return XmlUtil.GetElement(e, eleName);
        }

        private enum Type{
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

        protected static void LogNonAccessKey(string path, string val)
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

        public static void SwapAccessLog(string outfile = null)
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

    public class CsdBool : CsdType, ICsdParse<CsdBool>
    {
        public bool Value;
        public CsdBool Parse(XmlElement e)
        {
            Value = GetBoolAttribute(e, "Value");
            return this;
        }
        public static implicit operator bool(CsdBool t)
        {
            return t.Value;
        }
        public static implicit operator CsdBool(bool t)
        {
            return new CsdBool { Value = t };
        }
    }

    public class CsdInteger : CsdType, ICsdParse<CsdInteger>
    {
        public int Value;
        public CsdInteger Parse(XmlElement e)
        {
            Value = GetIntegerAttribute(e, "Value");
            return this;
        }
        public static implicit operator int(CsdInteger t)
        {
            return t.Value;
        }
        public static implicit operator CsdInteger(int t)
        {
            return new CsdInteger { Value = t };
        }
    }

    public class CsdFloat : CsdType, ICsdParse<CsdFloat>
    {
        public float Value;
        public CsdFloat Parse(XmlElement e)
        {
            Value = GetFloatAttribute(e, "Value");
            return this;
        }
        public static implicit operator float(CsdFloat t)
        {
            return t.Value;
        }
        public static implicit operator CsdFloat(float t)
        {
            return new CsdFloat { Value = t };
        }
    }

    public class CsdString : CsdType, ICsdParse<CsdString>
    {
        public string Value;
        public CsdString Parse(XmlElement e)
        {
            Value = GetStringAttribute(e, "Value");
            return this;
        }
        public static implicit operator string(CsdString t)
        {
            return t.Value;
        }
        public static implicit operator CsdString(string t)
        {
            return new CsdString { Value = t };
        }

    }


    public class CsdVector3 : CsdType, ICsdParse<CsdVector3>
    {
        public float X;
        public float Y;
        public float Z;

        public CsdVector3 Parse(XmlElement e)
        {
            return Parse(e, 0f);
        }
        public CsdVector3 Parse(XmlElement e, float defaultValue)
        {
            X = GetFloatAttribute(e, "X", GetFloatAttribute(e, "ScaleX", defaultValue));
            Y = GetFloatAttribute(e, "Y", GetFloatAttribute(e, "ScaleY", defaultValue));
            Z = GetFloatAttribute(e, "Z", GetFloatAttribute(e, "ScaleZ", defaultValue));
            return this;
        }
    }

    public class CsdColor : CsdType, ICsdParse<CsdColor>
    {
        public float R;
        public float G;
        public float B;
        public float A;
        public CsdColor Parse(XmlElement e)
        {
            R = GetIntegerAttribute(e, "R", 255) / 255f;
            G = GetIntegerAttribute(e, "G", 255) / 255f;
            B = GetIntegerAttribute(e, "B", 255) / 255f;
            A = GetIntegerAttribute(e, "A", 255) / 255f;
            return this;
        }
        public CsdColor Parse(XmlElement e, float Alpha)
        {
            Parse(e);
            Alpha = Alpha > 1f ? 1f : Alpha < 0f ? 0f : Alpha;
            A = Alpha;
            return this;
        }
    }

    public class CsdColorGradient : CsdType, ICsdParse<CsdColorGradient>
    {
        public enum ColorMode
        {
            None = 0,
            Color = 1,
            Gradient = 2,
        }
        public ColorMode Mode;
        public CsdColor Color;
        public CsdColor ColorA;
        public CsdColor ColorB;
        public CsdVector3 ColorVector;
        public CsdColorGradient Parse(XmlElement e)
        {
            Mode = (ColorMode)GetIntegerAttribute(e, "ComboBoxIndex", 0);
            var SingleColor = new CsdColor().Parse(GetElement(e, "SingleColor"));
            var FirstColor = new CsdColor().Parse(GetElement(e, "FirstColor"));
            var EndColor = new CsdColor().Parse(GetElement(e, "EndColor"));
            var Dir = new CsdVector3().Parse(GetElement(e, "ColorVector"));
            var Dir_ignored = GetFloatAttribute(e, "ColorAngle", 90f);  //same mean as ColorVector, but in degrees
            Color = SingleColor;
            ColorA = FirstColor;
            ColorB = EndColor;
            ColorVector = Dir;
            return this;
        }
        public CsdColorGradient Parse(XmlElement e, float Alpha)
        {
            Parse(e);
            Alpha = Alpha > 1f ? 1f : Alpha < 0f ? 0f : Alpha;
            if (Color != null) Color.A = Alpha;
            if (ColorA != null) ColorA.A = Alpha;
            if (ColorB != null) ColorB.A = Alpha;
            return this;
        }
    }

    public class CsdFileLink : CsdType, ICsdParse<CsdFileLink>
    {
        public string Type;
        public string Path;
        public string Plist;

        public CsdFileLink Parse(XmlElement e)
        {
            if (GetElement(e, "TextureFile") != null)
                e = GetElement(e, "TextureFile");
            Type = GetStringAttribute(e, "Type", null);
            Path = GetStringAttribute(e, "Path", null);
            Plist = GetStringAttribute(e, "Plist", null);
            return this;
        }
    }

    public enum CsdCurveType
    {
        Constant                = -2,
        Costum                  = -1,
        Linear                  = 0,
        EaseInSine              = 1,
        EaseOutSine             = 2,
        EaseInOutSine           = 3,
        EaseInQuad              = 4,
        EaseOutQuad             = 5,
        EaseInOutQuad           = 6,
        EaseInCubic             = 7,
        EaseOutCubic            = 8,
        EaseInOutCubic          = 9,
        EaseInQuart             = 10,
        EaseOutQuart            = 11,
        EaseInOutQuart          = 12,
        EaseInQuint             = 13,
        EaseOutQuint            = 14,
        EaseInOutQuint          = 15,
        EaseInExpo              = 16,
        EaseOutExpo             = 17,
        EaseInOutExpo           = 18,
        EaseInCirc              = 19,
        EaseOutCirc             = 20,
        EaseInOutCirc           = 21,
        EaseInElastic           = 22,
        EaseOutElastic          = 23,
        EaseInOutElastic        = 24,
        EaseInBack              = 25,
        EaseOutBack             = 26,
        EaseInOutBack           = 27,
        EaseInBounce            = 28,
        EaseOutBounce           = 29,
        EaseInOutBounce         = 30,
    }


    public class CsdFrame<T> : CsdType, ICsdParse<CsdFrame<T>> where T : ICsdParse<T>, new()
    {
        public int FrameIndex;
        public float FrameScale;
        public T Value;
        public CsdCurveType Type = CsdCurveType.Linear;
        public CubicBezier Bezier = CubicBezier.Linear;
        public float Time => FrameIndex * FrameScale;

        public CsdFrame<T> Parse(XmlElement e)
        {
            return Parse(e, 1 / 60f);
        }

        public CsdFrame<T> Parse(XmlElement e, float frameScale)
        {
            FrameIndex = GetIntegerAttribute(e, "FrameIndex");
            Value = new T().Parse(e);
            FrameScale = frameScale;
            if (!GetBoolAttribute(e, "Tween", true))
            {
                Type = CsdCurveType.Constant;
                Bezier = CubicBezier.Constant;
            }
            else if (GetElement(e, "EasingData") != null)
            {
                var val = GetIntegerAttribute(GetElement(e, "EasingData"), "Type", 0);
                if (Enum.IsDefined(typeof(CsdCurveType), val))
                {
                    Type = (CsdCurveType)val;
                    if (Type == CsdCurveType.Costum)
                    {
                        var points = GetElement(GetElement(e, "EasingData"), "Points");
                        var p1 = (XmlElement)points.ChildNodes[1];
                        var p2 = (XmlElement)points.ChildNodes[2];
                        var x1 = GetFloatAttribute(p1, "X");
                        var y1 = GetFloatAttribute(p1, "Y");
                        var x2 = GetFloatAttribute(p2, "X");
                        var y2 = GetFloatAttribute(p2, "Y");
                        x1 = Math.Max(x1, 0.0001f);
                        x2 = Math.Min(x2, 0.9999f);
                        Bezier = new CubicBezier(x1, y1, x2, y2);
                    }
                    else
                    {
                        string mode = Type.ToString();
                        if (!CubicBezier.GetPreset(mode, out Bezier)) Bezier = new CubicBezier(0, 0, 0, 0);
                    }
                }
                else
                    LogNonAccessKey("Frame.EasingData.Type", "Type_" + val.ToString());
            }
            return this;
        }
    }

    public class CsdNode : CsdType, ICsdParse<CsdNode>
    {
        public string Name;                                     // 名字
        public string Path = "";                                // 路径（debug）
        public string Tag;                                      // 唯一标签
        public string ActionTag;                                // 动画标签
        public bool isActive;                                   // 是否显示 / 激活
        public bool isInteractive;                              // 是否可交互
        public bool canEdit;                                    // 是否可被编辑（编辑器属性）
        public CsdVector3 Position;                             // 位置
        public CsdVector3 RotationSkew;                         // 旋转倾斜
        public CsdVector3 Scale;                                // 缩放
        public CsdVector3 Size;                                 // 大小
        public CsdVector3 Pivot;                                // 中心点位置
        public CsdVector3 Anchor;                               // 锚点位置
        public CsdColor Color;                                  // 主要颜色
        public List<CsdNode> Children;                          // 子节点列表
        public CsdFileLink FillImage;                           // 填充图片
        public CsdFileLink FillNode;                            // 填充其他节点
        public CsdColorGradient FillColor;                      // 填充背景颜色（渐变）

        public CsdNode Parse(XmlElement e)
        {
            return Parse(e, null);
        }
        public CsdNode Parse(XmlElement e, string pathPrefix)
        {
            Name = GetStringAttribute(e, "Name");
            Path = (pathPrefix != null || pathPrefix != "" ? (pathPrefix + "/") : "") + Name;
            Tag = GetStringAttribute(e, "Tag");
            ActionTag = GetStringAttribute(e, "ActionTag", null);
            isActive = GetBoolAttribute(e, "VisibleForFrame", true);
            isInteractive = GetBoolAttribute(e, "TouchEnable", false);
            canEdit = GetBoolAttribute(e, "CanEdit", true);
            Position = new CsdVector3().Parse(GetElement(e, "Position"));
            RotationSkew = GenRotationSkew(e);
            Scale = new CsdVector3().Parse(GetElement(e, "Scale"), 1f);
            Size = new CsdVector3().Parse(GetElement(e, "Size"));
            Pivot = new CsdVector3().Parse(GetElement(e, "AnchorPoint"), 0f);
            Anchor = GenAnchors(e);
            Color = new CsdColor().Parse(GetElement(e, "CColor"), GetIntegerAttribute(e, "Alpha", 255) / 255f);
            var FillType = GetStringAttribute(e, "ctype");
            switch (FillType)
            {
                case "GameNodeObjectData":
                    // mark the root, nothing to do
                    break;
                case "PanelObjectData":
                    FillColor = new CsdColorGradient().Parse(e, GetIntegerAttribute(e, "BackColorAlpha", 255) / 255f);
                    break;
                case "SpriteObjectData":
                    FillImage = new CsdFileLink().Parse(GetElement(e, "FileData"));
                    break;
                case "ProjectNodeObjectData":
                    FillNode = new CsdFileLink().Parse(GetElement(e, "FileData"));
                    break;
                default:
                    LogNonAccessKey("AbstractNodeData.ctype", FillType);
                    break;
            }
            var ChildrenElement = GetElement(e, "Children");
            if (ChildrenElement != null)
            {
                Children = new List<CsdNode>();
                foreach (XmlElement child in ChildrenElement)
                {
                    Children.Add(new CsdNode().Parse(child, Path));
                }
            }

            // ! debug log
            var deltaSkew = RotationSkew.X - RotationSkew.Y > 0 ? RotationSkew.X - RotationSkew.Y : RotationSkew.Y - RotationSkew.X;
            if (deltaSkew > 50) LogNonAccessKey("DeltaSkew", "Step50_");
            else if (deltaSkew > 30) {LogNonAccessKey("DeltaSkew", "Step30_50");LogNonAccessKey("#" + Path, "Step30_50");}
            else if (deltaSkew > 20) LogNonAccessKey("DeltaSkew", "Step20_30");
            else if (deltaSkew > 10) LogNonAccessKey("DeltaSkew", "Step10_20");
            else if (deltaSkew > 5) LogNonAccessKey("DeltaSkew", "Step05_10");
            else if (deltaSkew > 0) LogNonAccessKey("DeltaSkew", "Step00_05");
            if (Children != null)
            {
                if (Color.R != 1f || Color.G != 1f || Color.B != 1f) {LogNonAccessKey("ChildrenInherit", "ColorRGB");LogNonAccessKey("#" + Path, "ColorRGB");}
                if (Color.A != 1f) {LogNonAccessKey("ChildrenInherit", "ColorA");LogNonAccessKey("#" + Path, "ColorA");}
                if (deltaSkew > 3f) {LogNonAccessKey("ChildrenInherit", "DeltaSkew");LogNonAccessKey("#" + Path, "DeltaSkew");}
            }
            if (FillColor != null && FillColor.Mode == CsdColorGradient.ColorMode.Gradient) LogNonAccessKey("FillColor", "Gradient");

            // var IconVisible = GetBoolAttribute(e, "IconVisible", true);                                                         // Ignored
            // var PreSize = new CsdVector3().Parse(GetElement(e, "PreSize"));                                                     // Ignored
            // var PrePosition = new CsdVector3().Parse(GetElement(e, "PrePosition"));                                             // Ignored
            return this;
        }

        private static CsdVector3 GenAnchors(XmlElement e)
        {
            var Anchor = new CsdVector3();
            var HorizontalEdge = GetStringAttribute(e, "HorizontalEdge", "LeftEdge");
            var VerticalEdge = GetStringAttribute(e, "VerticalEdge", "BottomEdge");
            var LeftMargin = GetFloatAttribute(e, "LeftMargin", 0f);
            var RightMargin = GetFloatAttribute(e, "RightMargin", 0f);
            var TopMargin = GetFloatAttribute(e, "TopMargin", 0f);
            var BottomMargin = GetFloatAttribute(e, "BottomMargin", 0f);
            var Size = new CsdVector3().Parse(GetElement(e, "Size"));
            var Pivot = new CsdVector3().Parse(GetElement(e, "AnchorPoint"), 0f);
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
                    break;
            }
            return Anchor;
        }

        private static CsdVector3 GenRotationSkew(XmlElement e)
        {
            var RotationSkew = new CsdVector3();
            var RotationZ = GetFloatAttribute(e, "Rotation", 0f);  // ignored: always same as `RotationSkewX`
            var RotationSkewX = GetFloatAttribute(e, "RotationSkewX", 0f);
            var RotationSkewY = GetFloatAttribute(e, "RotationSkewY", 0f);
            // Rotation.Z = -RotationZ;
            RotationSkew.X = RotationSkewX;
            RotationSkew.Y = RotationSkewY;
            return RotationSkew;
        }

    }

    public class CsdTimeline : CsdType, ICsdParse<CsdTimeline>
    {
        public string ActionTag;                                // 动画标签
        public List<CsdFrame<CsdBool>> isActive;                // 是否显示 / 激活
        public List<CsdFrame<CsdVector3>> Position;             // 位置
        public List<CsdFrame<CsdVector3>> Rotation;             // 旋转
        public List<CsdFrame<CsdVector3>> Scale;                // 缩放
        public List<CsdFrame<CsdVector3>> Pivot;                // 中心点位置
        public List<CsdFrame<CsdFileLink>> Image;               // 链接图片
        public List<CsdFrame<CsdFloat>> Color_Alpha;            // 链接颜色（仅 Alpha）

        public CsdTimeline Parse(XmlElement e)
        {
            return Parse(e, 1 / 60f);
        }

        public CsdTimeline Parse(XmlElement e, float FrameScale)
        {
            ActionTag = GetStringAttribute(e, "ActionTag");
            var Property = GetStringAttribute(e, "Property");
            switch (Property)
            {
                case "Position":
                    Position = ParseFrameList<CsdVector3>(e, FrameScale);
                    break;
                case "Scale":
                    Scale = ParseFrameList<CsdVector3>(e, FrameScale);
                    break;
                case "AnchorPoint":
                    Pivot = ParseFrameList<CsdVector3>(e, FrameScale);
                    break;
                case "VisibleForFrame":
                    isActive = ParseFrameList<CsdBool>(e, FrameScale);
                    break;
                case "RotationSkew":
                    Rotation = ParseFrameList<CsdVector3>(e, FrameScale);
                    foreach(var f in Rotation)
                    {
                        var RotationSkew = f.Value;
                        var deltaSkew = RotationSkew.X - RotationSkew.Y > 0 ? RotationSkew.X - RotationSkew.Y : RotationSkew.Y - RotationSkew.X;
                        if (deltaSkew > 50) LogNonAccessKey("DeltaSkew_Animation", "Step50_");
                        else if (deltaSkew > 30) LogNonAccessKey("DeltaSkew_Animation", "Step30_50");
                        else if (deltaSkew > 20) LogNonAccessKey("DeltaSkew_Animation", "Step20_30");
                        else if (deltaSkew > 10) LogNonAccessKey("DeltaSkew_Animation", "Step10_20");
                        else if (deltaSkew > 5) LogNonAccessKey("DeltaSkew_Animation", "Step05_10");
                        else if (deltaSkew > 0) LogNonAccessKey("DeltaSkew_Animation", "Step00_05");
                    }
                    break;
                case "FileData":
                    Image = ParseFrameList<CsdFileLink>(e, FrameScale);
                    break;
                case "Alpha":
                    Color_Alpha = ParseFrameList<CsdFloat>(e, FrameScale);
                    foreach (var frame in Color_Alpha) frame.Value = frame.Value / 255f;
                    break;
                default:
                    LogNonAccessKey("Animation.Timeline.Property", Property);
                    break;
            }
            return this;
        }

        private static List<CsdFrame<T>> ParseFrameList<T>(XmlElement timeline, float FrameScale = 1 / 60f) where T : ICsdParse<T>, new()
        {
            List<CsdFrame<T>> Frames = new List<CsdFrame<T>>();
            foreach (XmlElement frame in timeline)
            {
                var Frame = new CsdFrame<T>().Parse(frame);
                Frame.FrameScale = FrameScale;
                Frames.Add(Frame);
            }
            return Frames;
        }

        public static Dictionary<string, CsdTimeline> ParseAll(XmlElement list, float FrameScale = 1 / 60f)
        {
            var map = new Dictionary<string, CsdTimeline>();
            foreach (XmlElement timeline in list)
            {
                var ActionTag = GetStringAttribute(timeline, "ActionTag");
                if (!map.TryGetValue(ActionTag, out var Timeline))
                {
                    Timeline = new CsdTimeline();
                    map.Add(ActionTag, Timeline);
                }
                Timeline.Parse(timeline, FrameScale);
            }
            return map;
        }
    }

    public class CsdAnimInfo : CsdType, ICsdParse<CsdAnimInfo>
    {
        public string Name;
        public int StartIndex;
        public int EndIndex;
        public float FrameScale;
        public float StartTime => StartIndex * FrameScale;
        public float EndTime => EndIndex * FrameScale;
        public CsdAnimInfo Parse(XmlElement e)
        {
            return Parse(e, 1 / 60f);
        }

        public CsdAnimInfo Parse(XmlElement e, float frameScale)
        {
            Name = GetStringAttribute(e, "Name");
            StartIndex = GetIntegerAttribute(e, "StartIndex");
            EndIndex = GetIntegerAttribute(e, "EndIndex");
            FrameScale = frameScale;
            return this;
        }
    }

    public class CsdParser : CsdType
    {
        public string Version;
        public string Name;
        public CsdNode Node;
        public Dictionary<string, CsdTimeline> Timelines;
        public Dictionary<string, CsdAnimInfo> Animations;
        public string DefaultAnimation = "";

        public CsdParser()
        {

        }
        public CsdParser(XmlDocument doc)
        {
            this.Parse(doc);
        }
        public CsdParser Parse(XmlDocument doc)
        {
            var root = doc["GameProjectFile"];

            var prop = root["PropertyGroup"];
            Name = prop.Attributes["Name"].Value;
            Version = prop.Attributes["Version"].Value;

            var content = root["Content"]?["Content"];
            if (content != null)
            {
                ParseObjectData(content["ObjectData"]);
                ParseAnimation(content["Animation"], out var FrameScale);
                ParseAnimationList(content["AnimationList"], FrameScale);
            }
            return this;
        }

        private void ParseObjectData(XmlElement ObjectData)
        {
            if (ObjectData == null)
            {
                throw new Exception("no ObjectData");
            }
            Node = new CsdNode().Parse(ObjectData, Name+"/");
        }
        private void ParseAnimation(XmlElement Animation, out float FrameScale)
        {
            if (Animation == null)
            {
                throw new Exception("no Animation");
            }
            var Duration = GetIntegerAttribute(Animation, "Duration");
            var Speed = GetFloatAttribute(Animation, "Speed");
            FrameScale = 1f / (Speed * 60);

            DefaultAnimation = GetStringAttribute(Animation, "ActivedAnimationName", "");
            Timelines = CsdTimeline.ParseAll(Animation, FrameScale);
            if (Timelines == null || Timelines.Count == 0)
            {
                Timelines = null;
            }
        }
        private void ParseAnimationList(XmlElement AnimationList, float FrameScale)
        {
            if (AnimationList != null)
            {
                Animations = new Dictionary<string, CsdAnimInfo>();
                foreach (XmlElement e in AnimationList)
                {
                    var info = new CsdAnimInfo().Parse(e, FrameScale);
                    Animations.Add(info.Name, info);
                }
            }
        }
    }
}
