using System;
using System.Collections.Generic;
using Wynncs.Entry;

namespace Cocos2Unity
{
    public class ModSpriteList
    {
        public ModString Name;
        public ModVector2 MaxTextureSize;
        public ModBoolean AllowRotation;
        public ModInt32 SpritePadding;
        public List<ModLink> SpriteList;

        public ModSpriteList()
        {
            this.Name = "";
            this.MaxTextureSize = new ModVector2(2048, 2048);
            this.AllowRotation = false;
            this.SpritePadding = 0;
            this.SpriteList = new List<ModLink>();
        }
        public void AddSprite(ModLink sprite)
        {
            this.SpriteList.Add(sprite);
        }
    }

    public class ModNode : IModTimeline
    {
        public ModString Name;
        public ModBoolean Visable;
        public ModBoolean Interactive;
        public ModRect Rect;
        public ModVector2 Pivot;
        public ModRect Anchor;
        public ModVector2 Skew;
        public ModVector2 Scale;
        public ModFiller Filler;
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
            this.Children = new List<ModNode>();
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

    public class ModNodeAnimation
    {
        public ModString Name;
        public ModSingle FrameRate = 60f;
        public ModSingle TimeFrom;
        public ModSingle TimeTo;
        public ModInt32 IndexFrom { get => (int)(TimeFrom * FrameRate + 0.5); set { TimeFrom = value / FrameRate; } }
        public ModInt32 IndexTo { get => (int)(TimeTo * FrameRate + 0.5); set { TimeTo = value / FrameRate; } }
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
            this.Name = "";
            this.TimeFrom = 0;
            this.TimeTo = 1;
            this.Timelines = new Dictionary<ModNode, ModTimeline<ModNode>>();
        }
    }


    public class ModNodePackage
    {
        public ModString Name;
        public ModNode Root;
        public Dictionary<string, > NodeDict;

        public List<ModNode> Nodes
        {
            get
            {
                var nodes = new List<ModNode>();
                if (Root != null)
                {
                    Root.GetChildrenRecursive(nodes);
                }
                return nodes;
            }
        }

    }
}
