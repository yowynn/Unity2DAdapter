using System;
using System.Collections.Generic;

namespace Cocos2Unity.Models
{
    public class ModNode : IModTimeline
    {
        public string Name;
        public ModBoolean Visible;
        public ModBoolean Interactive;
        public ModRect Rect;
        public ModVector2 Pivot;
        public ModRect Anchor;
        public ModVector2 Skew;
        public ModVector2 Scale;
        public ModFiller Filler;
        public ModColor Color;
        public List<ModNode> Children;
        public ModNode Parent;

        public string Path
        {
            get
            {
                if (Parent == null)
                    return Name;
                else
                    return Parent.Path + "/" + Name;
            }
        }

        public bool IsRoot
        {
            get
            {
                return Parent == null;
            }
        }

        public bool HasChildren
        {
            get
            {
                return Children != null && Children.Count > 0;
            }
        }

        public ModNode()
        {
            Name = "";
            Visible = true;
            Interactive = true;
            Rect = new ModRect(0, 0, 0, 0);
            Pivot = new ModVector2(0.5f, 0.5f);
            Anchor = new ModRect(0, 0, 0, 0);
            Skew = new ModVector2(0, 0);
            Scale = new ModVector2(1, 1);
            Filler = new ModFiller(ModFiller.ModType.None);
            Color = new ModColor(255, 255, 255, 255);
            Children = new List<ModNode>();
            Parent = null;
        }

        public void AddChild(ModNode child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        public void GetChildrenRecursive(List<ModNode> children)
        {
            children.Add(this);
            if (HasChildren)
            {
                foreach (ModNode child in Children)
                {
                    child.GetChildrenRecursive(children);
                }
            }
        }

        public void GetLinkedNodes(Dictionary<ModNode, ModLinkedAsset> linkedNodes)
        {
            if (Filler.Type == ModFiller.ModType.Node)
            {
                linkedNodes.Add(this, Filler.Node);
            }

            if (HasChildren)
            {
                foreach (ModNode child in Children)
                {
                    child.GetLinkedNodes(linkedNodes);
                }
            }
        }

        public void GetLinkedSprites(Dictionary<ModNode, ModLinkedAsset> linkedSprites)
        {
            if (Filler.Type == ModFiller.ModType.Sprite)
            {
                linkedSprites.Add(this, Filler.Sprite);
            }

            if (HasChildren)
            {
                foreach (ModNode child in Children)
                {
                    child.GetLinkedSprites(linkedSprites);
                }
            }
        }

    }

    public class ModNodeAnimationAtlas
    {
        private struct AnimationInfo
        {
            public string Name;
            public float TimeFrom;
            public float TimeTo;
        }
        private float frameRate;
        private Dictionary<ModNode, ModTimeline<ModNode>> timelines;
        private Dictionary<string, AnimationInfo> animationInfos;
        public float FrameRate { get => frameRate; private set => frameRate = value; }

        public float Duration
        {
            get
            {
                float duration = 0f;
                foreach (ModTimeline<ModNode> timeline in timelines.Values)
                {
                    duration = Math.Max(duration, timeline.Duration);
                }
                return duration;
            }
        }

        public Dictionary<string, ModNodeAnimation> Animations
        {
            get
            {
                Dictionary<string, ModNodeAnimation> animations = new Dictionary<string, ModNodeAnimation>();
                foreach (string name in GetAnimationNames())
                {
                    animations.Add(name, GetAnimation(name));
                }
                return animations;
            }
        }
        public ModNodeAnimationAtlas(float frameRate = 60f)
        {
            FrameRate = frameRate;
            timelines = new Dictionary<ModNode, ModTimeline<ModNode>>();
            animationInfos = new Dictionary<string, AnimationInfo>();
        }

        public ModTimeline<ModNode> AddTimeline(ModNode node)
        {
            if (timelines.ContainsKey(node))
            {
                return timelines[node];
            }
            else
            {
                ModTimeline<ModNode> timeline = new ModTimeline<ModNode>(FrameRate);
                timelines.Add(node, timeline);
                return timeline;
            }
        }

        public ModTimeline<ModNode> GetTimeline(ModNode node)
        {
            if (timelines.ContainsKey(node))
            {
                return timelines[node];
            }
            else
            {
                return null;
            }
        }

        public ModNodeAnimation AddAnimation(string name, float timeFrom, float timeTo)
        {
            AnimationInfo info = new AnimationInfo();
            info.Name = name;
            info.TimeFrom = timeFrom;
            info.TimeTo = timeTo;
            animationInfos.Add(name, info);
            return GetAnimation(name);
        }

        public ModNodeAnimation AddAnimation(string name, int indexFrom, int indexTo)
        {
            AnimationInfo info = new AnimationInfo();
            info.Name = name;
            info.TimeFrom = indexFrom / FrameRate;
            info.TimeTo = indexTo / FrameRate;
            animationInfos.Add(name, info);
            return GetAnimation(name);
        }

        public ModNodeAnimation GetAnimation(string name)
        {
            if (animationInfos.TryGetValue(name, out AnimationInfo info))
            {
                return new ModNodeAnimation
                {
                    Name = name,
                    FrameRate = FrameRate,
                    TimeFrom = info.TimeFrom,
                    TimeTo = info.TimeTo,
                    AnimationAtlas = this,
                };
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<string> GetAnimationNames()
        {
            return animationInfos.Keys;
        }

        public IEnumerable<ModNode> GetAnimatedNodes()
        {
            return timelines.Keys;
        }
    }

    public class ModNodeAnimation
    {
        public string Name;
        public float FrameRate;
        public float TimeFrom;
        public float TimeTo;
        public int IndexFrom { get => (int)(TimeFrom * FrameRate + 0.5); set { TimeFrom = value / FrameRate; } }
        public int IndexTo { get => (int)(TimeTo * FrameRate + 0.5); set { TimeTo = value / FrameRate; } }
        public ModNodeAnimationAtlas AnimationAtlas;

        public float Duration
        {
            get
            {
                return TimeTo - TimeFrom;
            }
        }

        public ModNodeAnimation()
        {
        }
    }
}
