using System;
using System.Collections.Generic;
using System.Xml;
using Unity2DAdapter.Models;
using Unity2DAdapter.Util;

namespace Unity2DAdapter.CocoStudio
{
    public static class CsdParser
    {
        public static NodePackage ParseCsd(string filepath)
        {
            var TARGET = new NodePackage();
            var file = new XmlDocument();
            file.Load(filepath);
            var root = file.DocumentElement;

            var PropertyGroup = root.GetElement("PropertyGroup");
            var Name = PropertyGroup.GetStringAttribute("Name");
            TARGET.Name = Name;

            var Content = root.GetElement("Content").GetElement("Content");
            var ObjectData = Content.GetElement("ObjectData");
            var Animation = Content.GetElement("Animation");
            var AnimationList = Content.GetElement("AnimationList");
            var animatedNodes = new Dictionary<string, ModNode>();
            ModNode node = ParseNode(ObjectData, animatedNodes);
            ModNodeAnimationAtlas atlas = ParseTimeline(Animation, animatedNodes, out string defaultAnimationName);
            FillAnimationInfos(AnimationList, atlas);
            TARGET.RootNode = node;
            TARGET.DefaultAnimationName = defaultAnimationName;
            TARGET.AddAtlas(atlas);

            return TARGET;
        }

        private static ModVector2 GetModVector2(XmlElement node, float defaultValue = 0f)
        {
            var X = node.GetFloatAttribute("X", defaultValue);
            var Y = node.GetFloatAttribute("Y", defaultValue);
            return new ModVector2(X, Y);
        }

        private static ModVector2 GetModVector2Scale(XmlElement node, float defaultValue = 0f)
        {
            var X = node.GetFloatAttribute("ScaleX", defaultValue);
            var Y = node.GetFloatAttribute("ScaleY", defaultValue);
            return new ModVector2(X, Y);
        }

        private static ModColor GetModColor(XmlElement node, int defaultValue = 255)
        {
            var R = node.GetIntegerAttribute("R", defaultValue);
            var G = node.GetIntegerAttribute("G", defaultValue);
            var B = node.GetIntegerAttribute("B", defaultValue);
            var A = node.GetIntegerAttribute("A", defaultValue);
            return new ModColor(R, G, B, A);
        }

        private static ModLinkedAsset GetModLink(XmlElement node)
        {
            var Path = node.GetStringAttribute("Path");
            var Plist = node.GetStringAttribute("Plist");
            if (string.IsNullOrEmpty(Plist))
            {
                Plist = null;
            }
            return new ModLinkedAsset(Path, Plist);
        }

        private static ModNode ParseNode(XmlElement node, Dictionary<string, ModNode> animatedNodes, ModNode parent = null)
        {
            var TARGET = new ModNode();
            if (parent != null)
            {
                parent.AddChild(TARGET);
            }
            TARGET.Name = node.GetStringAttribute("Name");
            TARGET.Visible = node.GetBoolAttribute("VisibleForFrame", true);
            TARGET.Interactive = node.GetBoolAttribute("TouchEnable", false);
            TARGET.Rect = ParseNodeRect(node);
            TARGET.Pivot = ParseNodePivot(node);
            TARGET.Anchor = ParseNodeAnchor(node);
            TARGET.Skew = ParseNodeSkew(node);
            TARGET.Scale = ParseNodeScale(node);
            TARGET.Filler = ParseNodeFiller(node);
            TARGET.Color = ParseNodeColor(node);

            var Children = node.GetElement("Children");
            if (Children != null)
            {
                foreach (XmlElement child in Children)
                {
                    var childNode = ParseNode(child, animatedNodes, TARGET);
                }
            }

            var ActionTag = node.GetStringAttribute("ActionTag");
            if (!string.IsNullOrEmpty(ActionTag))
            {
                animatedNodes.Add(ActionTag, TARGET);
            }

            return TARGET;
        }

        private static ModRect ParseNodeRect(XmlElement node)
        {
            var TARGET = new ModRect();
            var Position = node.GetElement("Position");
            TARGET.Position = GetModVector2(Position, 0f);
            var Size = node.GetElement("Size");
            TARGET.Size = GetModVector2(Size, 0f);
            return TARGET;
        }

        private static ModVector2 ParseNodePivot(XmlElement node)
        {
            var AnchorPoint = node.GetElement("AnchorPoint");
            var TARGET = GetModVector2Scale(AnchorPoint, 0f);
            return TARGET;
        }

        private static ModRect ParseNodeAnchor(XmlElement node)
        {
            var TARGET = new ModRect();
            var Size = ParseNodeRect(node).Size;
            var Pivot = ParseNodePivot(node);
            var HorizontalEdge = node.GetStringAttribute("HorizontalEdge", "LeftEdge");
            var VerticalEdge = node.GetStringAttribute("VerticalEdge", "BottomEdge");
            var LeftMargin = node.GetFloatAttribute("LeftMargin", 0f);
            var RightMargin = node.GetFloatAttribute("RightMargin", 0f);
            var TopMargin = node.GetFloatAttribute("TopMargin", 0f);
            var BottomMargin = node.GetFloatAttribute("BottomMargin", 0f);

            float X = 0f, Y = 0f;
            switch (HorizontalEdge)
            {
                case "LeftEdge":
                    X = 0f;
                    break;
                case "RightEdge":
                    X = 1f;
                    break;
                case "BothEdge":
                    X = (Size.X * Pivot.X + LeftMargin) / (Size.X + LeftMargin + RightMargin);
                    break;
            }
            switch (VerticalEdge)
            {
                case "BottomEdge":
                    Y = 0f;
                    break;
                case "TopEdge":
                    Y = 1f;
                    break;
                case "BothEdge":
                    Y = (Size.Y * Pivot.Y + BottomMargin) / (Size.Y + TopMargin + BottomMargin);
                    break;
            }

            TARGET.Min = new ModVector2(X, Y);
            TARGET.Max = new ModVector2(X, Y);
            return TARGET;
        }

        private static ModVector2 ParseNodeSkew(XmlElement node)
        {
            var TARGET = new ModVector2();
            var RotationSkewX = node.GetFloatAttribute("RotationSkewX", 0f);
            var RotationSkewY = node.GetFloatAttribute("RotationSkewY", 0f);
            TARGET.X = RotationSkewX;
            TARGET.Y = RotationSkewY;
            return TARGET;
        }

        private static ModVector2 ParseNodeScale(XmlElement node)
        {
            var Scale = node.GetElement("Scale");
            var TARGET = GetModVector2Scale(Scale, 1f);
            return TARGET;
        }

        private static ModFiller ParseNodeFiller(XmlElement node)
        {
            ModFiller TARGET = null;
            var ctype = node.GetStringAttribute("ctype");
            switch (ctype)
            {
                case "GameNodeObjectData":
                    // fill nothing
                    TARGET = new ModFiller(ModFiller.ModType.None);
                    break;
                case "PanelObjectData":
                    // fill color
                    ModColorVector Color = ParseNodeFillerColor(node);
                    TARGET = new ModFiller(ModFiller.ModType.Color, Color);
                    break;
                case "SpriteObjectData":
                    // fill sprite
                    ModLinkedAsset Sprite = ParseNodeFillerSprite(node);
                    TARGET = new ModFiller(ModFiller.ModType.Sprite, Sprite);
                    break;
                case "ProjectNodeObjectData":
                    // fill node
                    ModLinkedAsset Node = ParseNodeFillerNode(node);
                    TARGET = new ModFiller(ModFiller.ModType.Node, Node);
                    break;
                default:
                    TARGET = new ModFiller(ModFiller.ModType.None);
                    XmlAnalyzer.LogNonAccessKey("AbstractNodeData.ctype", ctype);
                    break;
            }
            return TARGET;
        }

        private static ModColorVector ParseNodeFillerColor(XmlElement node)
        {
            ModColorVector TARGET = null;
            var ComboBoxIndex = node.GetIntegerAttribute("ComboBoxIndex", 0);
            var SingleColor = node.GetElement("SingleColor");
            var FirstColor = node.GetElement("FirstColor");
            var EndColor = node.GetElement("EndColor");
            var ColorVector = node.GetElement("ColorVector");
            var BackColorAlpha = node.GetIntegerAttribute("BackColorAlpha", 255);

            switch (ComboBoxIndex)
            {
                case 0:
                    // None color
                    TARGET = new ModColorVector();
                    break;
                case 1:
                    // Solid color
                    var color = GetModColor(SingleColor);
                    color.A = BackColorAlpha / 255f;
                    TARGET = new ModColorVector(color);
                    break;
                case 2:
                    // Gradient color
                    var color1 = GetModColor(FirstColor);
                    color1.A = BackColorAlpha / 255f;
                    var color2 = GetModColor(EndColor);
                    color2.A = BackColorAlpha / 255f;
                    var direction = GetModVector2Scale(ColorVector, 0f);
                    TARGET = new ModColorVector(color1, color2, direction);
                    break;
                default:
                    XmlAnalyzer.LogNonAccessKey("AbstractNodeData.ComboBoxIndex", ComboBoxIndex.ToString());
                    break;
            }
            return TARGET;
        }

        private static ModLinkedAsset ParseNodeFillerSprite(XmlElement node)
        {
            var FileData = node.GetElement("FileData");
            var TARGET = GetModLink(FileData);
            return TARGET;
        }

        private static ModLinkedAsset ParseNodeFillerNode(XmlElement node)
        {
            var FileData = node.GetElement("FileData");
            var TARGET = GetModLink(FileData);
            return TARGET;
        }

        private static ModColor ParseNodeColor(XmlElement node)
        {
            var Color = node.GetElement("CColor");
            var Alpha = node.GetIntegerAttribute("Alpha", 255);
            var TARGET = GetModColor(Color);
            TARGET.A = Alpha / 255f;
            return TARGET;
        }

        private static ModNodeAnimationAtlas ParseTimeline(XmlElement list, Dictionary<string, ModNode> animatedNodes, out string defaultAnimation)
        {
            var Speed = list.GetFloatAttribute("Speed", 1f);
            var FrameRate = Speed * 60f;
            var TARGET = new ModNodeAnimationAtlas(FrameRate);
            foreach (XmlElement Timeline in list)
            {
                var ActionTag = Timeline.GetStringAttribute("ActionTag");
                var node = animatedNodes[ActionTag];
                var timeline = TARGET.GetTimeline(node);
                if (timeline == null)
                {
                    timeline = TARGET.AddTimeline(node);
                }
                FillTimelineCurve(timeline, Timeline, node);
            }
            defaultAnimation = list.GetStringAttribute("ActivedAnimationName");
            if (string.IsNullOrEmpty(defaultAnimation))
            {
                defaultAnimation = null;
            }
            return TARGET;
        }

        private static void FillTimelineCurve(ModTimeline<ModNode> timeline, XmlElement list, ModNode nodeToFixOriginData = null)
        {
            var Property = list.GetStringAttribute("Property");
            switch (Property)
            {
                case "Position":
                    var curvePosition = timeline.AddCurve<ModVector2>("Rect.Position");
                    FillTimelineCurveFrames(curvePosition, list, e => GetModVector2(e));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Rect.Position = curvePosition.KeyFrames[0].Value;
                    break;
                case "Scale":
                    var curveScale = timeline.AddCurve<ModVector2>("Scale");
                    FillTimelineCurveFrames(curveScale, list, e => GetModVector2(e));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Scale = curveScale.KeyFrames[0].Value;
                    break;
                case "AnchorPoint":
                    var curvePivot = timeline.AddCurve<ModVector2>("Pivot");
                    FillTimelineCurveFrames(curvePivot, list, e => GetModVector2(e));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Pivot = curvePivot.KeyFrames[0].Value;
                    break;
                case "VisibleForFrame":
                    var curveVisible = timeline.AddCurve<ModBoolean>("Visible");
                    FillTimelineCurveFrames(curveVisible, list, e => e.GetBoolAttribute("Value"));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Visible = curveVisible.KeyFrames[0].Value;
                    break;
                case "RotationSkew":
                    var curveSkew = timeline.AddCurve<ModVector2>("Skew");
                    FillTimelineCurveFrames(curveSkew, list, e => GetModVector2(e));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Skew = curveSkew.KeyFrames[0].Value;
                    break;
                case "FileData":
                    var curveSprite = timeline.AddCurve<ModLinkedAsset>("Filler.Sprite");
                    FillTimelineCurveFrames(curveSprite, list, e => GetModLink(e.GetElement("TextureFile")));
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Filler.Sprite = curveSprite.KeyFrames[0].Value;
                    break;
                case "Alpha":
                    var curveAlpha = timeline.AddCurve<ModSingle>("Color.A");
                    FillTimelineCurveFrames(curveAlpha, list, e => e.GetIntegerAttribute("Value") / 255f);
                    if (nodeToFixOriginData != null) nodeToFixOriginData.Color.A = curveAlpha.KeyFrames[0].Value;
                    break;
                default:
                    XmlAnalyzer.LogNonAccessKey("Animation.Timeline.Property", Property);
                    break;
            }
        }

        private static void FillTimelineCurveFrames<ModType>(ModCurve<ModType> curve, XmlElement list, Func<XmlElement, ModType> parser) where ModType : ModBase
        {
            foreach (XmlElement Frame in list)
            {
                var FrameIndex = Frame.GetIntegerAttribute("FrameIndex");
                var Value = parser(Frame);
                var frame = curve.AddFrame(FrameIndex, Value);
                var Tween = Frame.GetBoolAttribute("Tween", true);
                if (!Tween)
                {
                    frame.Transition = CubicBezier.Constant;
                }
                else
                {
                    var EasingData = Frame.GetElement("EasingData");
                    frame.Transition = ParseTimelineCurveFrameTransition(EasingData);
                }
            }
            // handle neigbour frames as constant
            var frames = curve.KeyFrames;
            if (frames.Count > 1)
            {
                for (int i = frames.Count - 2; i >= 0; --i)
                {
                    if (frames[i].Index == frames[i + 1].Index - 1)
                    {
                        frames[i].Transition = CubicBezier.Constant;
                    }
                }
            }
        }

        private static CubicBezier ParseTimelineCurveFrameTransition(XmlElement data)
        {
            if (data == null) return CubicBezier.Linear;
            var Type = data.GetIntegerAttribute("Type");
            switch (Type)
            {
                case -1:
                    // Costum
                    var Points = data.GetElement("Points");
                    var Point1 = (XmlElement)Points.ChildNodes[1];
                    var Point2 = (XmlElement)Points.ChildNodes[2];
                    var X1 = Point1.GetFloatAttribute("X", 0f);
                    var Y1 = Point1.GetFloatAttribute("Y", 0f);
                    var X2 = Point2.GetFloatAttribute("X", 0f);
                    var Y2 = Point2.GetFloatAttribute("Y", 0f);
                    X1 = Math.Max(X1, 0.0001f);                 // to make sure that the curve has a positive weight
                    X2 = Math.Min(X2, 0.9999f);                 // to make sure that the curve has a positive weight
                    return new CubicBezier(X1, Y1, X2, Y2);
                case 0:
                    // Linear
                    return CubicBezier.Linear;
                case 1:
                    // EaseInSine
                    return CubicBezier.EaseInSine;
                case 2:
                    // EaseOutSine
                    return CubicBezier.EaseOutSine;
                case 3:
                    // EaseInOutSine
                    return CubicBezier.EaseInOutSine;
                case 4:
                    // EaseInQuad
                    return CubicBezier.EaseInQuad;
                case 5:
                    // EaseOutQuad
                    return CubicBezier.EaseOutQuad;
                case 6:
                    // EaseInOutQuad
                    return CubicBezier.EaseInOutQuad;
                case 7:
                    // EaseInCubic
                    return CubicBezier.EaseInCubic;
                case 8:
                    // EaseOutCubic
                    return CubicBezier.EaseOutCubic;
                case 9:
                    // EaseInOutCubic
                    return CubicBezier.EaseInOutCubic;
                case 10:
                    // EaseInQuart
                    return CubicBezier.EaseInQuart;
                case 11:
                    // EaseOutQuart
                    return CubicBezier.EaseOutQuart;
                case 12:
                    // EaseInOutQuart
                    return CubicBezier.EaseInOutQuart;
                case 13:
                    // EaseInQuint
                    return CubicBezier.EaseInQuint;
                case 14:
                    // EaseOutQuint
                    return CubicBezier.EaseOutQuint;
                case 15:
                    // EaseInOutQuint
                    return CubicBezier.EaseInOutQuint;
                case 16:
                    // EaseInExpo
                    return CubicBezier.EaseInExpo;
                case 17:
                    // EaseOutExpo
                    return CubicBezier.EaseOutExpo;
                case 18:
                    // EaseInOutExpo
                    return CubicBezier.EaseInOutExpo;
                case 19:
                    // EaseInCirc
                    return CubicBezier.EaseInCirc;
                case 20:
                    // EaseOutCirc
                    return CubicBezier.EaseOutCirc;
                case 21:
                    // EaseInOutCirc
                    return CubicBezier.EaseInOutCirc;
                case 22:
                    // EaseInElastic
                    // return CubicBezier.EaseInElastic;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseInElastic");
                    return CubicBezier.Linear;

                case 23:
                    // EaseOutElastic
                    // return CubicBezier.EaseOutElastic;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseOutElastic");
                    return CubicBezier.Linear;
                case 24:
                    // EaseInOutElastic
                    // return CubicBezier.EaseInOutElastic;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseInOutElastic");
                    return CubicBezier.Linear;
                case 25:
                    // EaseInBack
                    return CubicBezier.EaseInBack;
                case 26:
                    // EaseOutBack
                    return CubicBezier.EaseOutBack;
                case 27:
                    // EaseInOutBack
                    return CubicBezier.EaseInOutBack;
                case 28:
                    // EaseInBounce
                    // return CubicBezier.EaseInBounce;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseInBounce");
                    return CubicBezier.Linear;
                case 29:
                    // EaseOutBounce
                    // return CubicBezier.EaseOutBounce;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseOutBounce");
                    return CubicBezier.Linear;
                case 30:
                    // EaseInOutBounce
                    // return CubicBezier.EaseInOutBounce;
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", "EaseInOutBounce");
                    return CubicBezier.Linear;
                default:
                    XmlAnalyzer.LogNonAccessKey("EasingData.Type", Type.ToString());
                    return CubicBezier.Linear;
            }
        }

        private static void FillAnimationInfos(XmlElement list, ModNodeAnimationAtlas atlas)
        {
            if (list != null)
            {
                foreach (XmlElement info in list)
                {
                    var Name = info.GetStringAttribute("Name");
                    var StartIndex = info.GetIntegerAttribute("StartIndex");
                    var EndIndex = info.GetIntegerAttribute("EndIndex");
                    atlas.AddAnimation(Name, StartIndex, EndIndex);
                }
            }
        }
    }
}
