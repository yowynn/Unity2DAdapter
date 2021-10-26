using System;
using Cocos2Unity.Models;
using System.Xml;
using System.Collections.Generic;

namespace Cocos2Unity.CocoStudio
{
    public partial class CocoStudioParser
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
            ModNodeAnimationAtlas atlas = ParseNodeAnimationAtlas(Animation, animatedNodes);
            var animationList = ParseAnimationList(AnimationList);

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

        private static ModLink GetModLink(XmlElement node)
        {
            var Path = node.GetStringAttribute("Path");
            var Plist = node.GetStringAttribute("Plist");
            if (string.IsNullOrEmpty(Plist))
            {
                Plist = null;
            }
            return new ModLink(Path, Plist);
        }


        private static ModNode ParseNode(XmlElement node, Dictionary<string, ModNode> animatedNodes)
        {
            var TARGET = new ModNode();
            TARGET.Name = node.GetStringAttribute("Name");
            TARGET.Visable = node.GetBoolAttribute("VisibleForFrame", true);
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
                    var childNode = ParseNode(child, animatedNodes);
                    TARGET.AddChild(childNode);
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
                    ModLink Sprite = ParseNodeFillerSprite(node);
                    TARGET = new ModFiller(ModFiller.ModType.Sprite, Sprite);
                    break;
                case "ProjectNodeObjectData":
                    // fill node
                    ModLink Node = ParseNodeFillerNode(node);
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

        private static ModLink ParseNodeFillerSprite(XmlElement node)
        {
            var FileData = node.GetElement("FileData");
            var TARGET = GetModLink(FileData);
            return TARGET;
        }

        private static ModLink ParseNodeFillerNode(XmlElement node)
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

        private static ModNodeAnimationAtlas ParseNodeAnimationAtlas(XmlElement list, Dictionary<string, ModNode> animatedNodes)
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
                FillTimelineCurve(timeline, Timeline);
                var Property = Timeline.GetStringAttribute("Property");
            }

            return TARGET;
        }

        private static void FillTimelineCurve(ModTimeline<ModNode> timeline, XmlElement list)
        {
            var Property = list.GetStringAttribute("Property");
            switch (Property)
            {
                case "Position":
                    var curve = timeline.AddCurve<ModVector2>("Rect.Position");
                    Position = ParseFrameList<CsdVector3>(e, FrameScale);
                    break;
                case "Scale":
                    Scale = ParseFrameList<CsdVector3>(e, FrameScale);
                    //! Z is allways 1!
                    foreach (var f in Scale)
                    {
                        var Scale = f.Value;
                        Scale.Z = 1;
                    }
                    break;
                case "AnchorPoint":
                    Pivot = ParseFrameList<CsdVector3>(e, FrameScale);
                    break;
                case "VisibleForFrame":
                    isActive = ParseFrameList<CsdBool>(e, FrameScale);
                    break;
                case "RotationSkew":
                    RotationSkew = ParseFrameList<CsdVector3>(e, FrameScale);
                    foreach (var f in RotationSkew)
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
                    FillImage = ParseFrameList<CsdFileLink>(e, FrameScale);
                    break;
                case "Alpha":
                    Color_Alpha = ParseFrameList<CsdFloat>(e, FrameScale);
                    foreach (var frame in Color_Alpha) frame.Value = frame.Value / 255f;
                    break;
                default:
                    LogNonAccessKey("Animation.Timeline.Property", Property);
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
                if (Tween)
                {
                    frame.Transition = CubicBezier.Constant;
                }
            }
        }
    }
}
