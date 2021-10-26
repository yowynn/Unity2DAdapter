using System;
using System.Collections.Generic;

namespace Cocos2Unity.Models
{
    public class ModNode : IModTimeline
    {
        public string Name;
        public ModBoolean Visable;
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

        public bool IsRoot
        {
            get
            {
                return this.Parent == null;
            }
        }

        public bool HasChildren
        {
            get
            {
                return this.Children != null && this.Children.Count > 0;
            }
        }

        public ModNode()
        {
            this.Name = "";
            this.Visable = true;
            this.Interactive = true;
            this.Rect = new ModRect(0, 0, 0, 0);
            this.Pivot = new ModVector2(0.5f, 0.5f);
            this.Anchor = new ModRect(0, 0, 0, 0);
            this.Skew = new ModVector2(0, 0);
            this.Scale = new ModVector2(1, 1);
            this.Filler = new ModFiller(ModFiller.ModType.None);
            this.Color = new ModColor(255, 255, 255, 255);
            this.Children = new List<ModNode>();
            this.Parent = null;
        }

        public void AddChild(ModNode child)
        {
            this.Children.Add(child);
            child.Parent = this;
        }

        public void GetChildrenRecursive(List<ModNode> children)
        {
            children.Add(this);
            if (this.HasChildren)
            {
                foreach (ModNode child in this.Children)
                {
                    child.GetChildrenRecursive(children);
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
                    Timelines = timelines,
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
        public Dictionary<ModNode, ModTimeline<ModNode>> Timelines;

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
