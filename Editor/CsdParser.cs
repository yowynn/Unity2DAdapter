using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

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
            string val = GetAttribute(e, attrName)?.Value;
            if (val == "True") return true;
            if (val == "False") return false;
            return defaultValue;
        }
        public static string GetStringAttribute(XmlElement e, string attrName, string defaultValue = "")
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = GetAttribute(e, attrName)?.Value;
            if (val != null) return val;
            return defaultValue;
        }
        public static float GetFloatAttribute(XmlElement e, string attrName, float defaultValue = 0f)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = GetAttribute(e, attrName)?.Value;
            if (val != null) return float.Parse(val);
            return defaultValue;
        }
        public static int GetIntegerAttribute(XmlElement e, string attrName, int defaultValue = 0)
        {
            if (e == null) return defaultValue;
            AccessStatAttribute(e, attrName);
            string val = GetAttribute(e, attrName)?.Value;
            if (val != null) return int.Parse(val);
            return defaultValue;
        }

        public static XmlAttribute GetAttribute(XmlElement e, string attrName)
        {
            return e?.Attributes?[attrName];
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
                XmlDocument log = new XmlDocument();
                var root = log.CreateElement("Root");
                log.AppendChild(root);
                var elements = log.CreateElement("Elements");
                root.AppendChild(elements);
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
                                elements.AppendChild(e);
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
                var logd = log.CreateElement("Logged");
                root.AppendChild(logd);
                foreach (var pair in LogedSet)
                {
                    var item = log.CreateElement("Item");
                    logd.AppendChild(item);
                    var path = log.CreateAttribute("_Path");
                    path.Value = pair.Key;
                    item.Attributes.SetNamedItem(path);
                    foreach (var pair0 in pair.Value)
                    {
                        var a = log.CreateAttribute(pair0.Key);
                        a.Value = pair0.Value.ToString();
                        item.Attributes.SetNamedItem(a);
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

    public class CsdFrame<T> : CsdType, ICsdParse<CsdFrame<T>> where T : ICsdParse<T>, new()
    {
        public enum EasingType
        {
            Constant = -2,
            Costum = -1,
            Linear = 0,
        }
        public int FrameIndex;
        public float FrameScale;
        public T Value;
        public EasingType Type = EasingType.Linear;
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
                Type = EasingType.Constant;
            }
            else if (GetElement(e, "EasingData") != null)
            {
                var val = GetIntegerAttribute(GetElement(e, "EasingData"), "Type", 0);
                if (Enum.IsDefined(typeof(EasingType), val))
                    Type = (EasingType)val;
                else
                    LogNonAccessKey("Frame.EasingData.Type", "Type_" + val.ToString());
            }
            return this;
        }
    }

    public class CsdNode : CsdType, ICsdParse<CsdNode>
    {
        public string Name;                                     // 名字
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
            Name = GetStringAttribute(e, "Name");
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
                    Children.Add(new CsdNode().Parse(child));
                }
            }

            var IconVisible = GetBoolAttribute(e, "IconVisible", true);                                                         // Ignored
            var PreSize = new CsdVector3().Parse(GetElement(e, "PreSize"));                                                     // Ignored
            var PrePosition = new CsdVector3().Parse(GetElement(e, "PrePosition"));                                             // Ignored
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
                    break;
                case "FileData":
                    Image = ParseFrameList<CsdFileLink>(e, FrameScale);
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
            Node = new CsdNode().Parse(ObjectData);
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
        }
        private void ParseAnimationList(XmlElement AnimationList, float FrameScale)
        {
            Animations = new Dictionary<string, CsdAnimInfo>();
            if (AnimationList != null)
            {
                foreach (XmlElement e in AnimationList)
                {
                    var info = new CsdAnimInfo().Parse(e, FrameScale);
                    Animations.Add(info.Name, info);
                }
            }
        }
    }
}
