using System.Collections.Generic;

namespace Cocos2Unity.Models
{
    public class NodePackage
    {
        private string name;
        private ModNode rootNode;
        private List<ModNodeAnimationAtlas> atlasList;
        private string defaultAnimationName;
        public string Name { get => name; set => name = value; }
        public ModNode RootNode { get => rootNode; set => rootNode = value; }
        public string DefaultAnimationName { get => defaultAnimationName; set => defaultAnimationName = value; }
        public List<ModNode> Nodes
        {
            get
            {
                var nodes = new List<ModNode>();
                if (RootNode != null)
                {
                    RootNode.GetChildrenRecursive(nodes);
                }
                return nodes;
            }
        }
        public Dictionary<string, ModNodeAnimation> Animations
        {
            get
            {
                var animations = new Dictionary<string, ModNodeAnimation>();
                if (atlasList != null)
                {
                    foreach (var atlas in atlasList)
                    {
                        foreach (string name in atlas.GetAnimationNames())
                        {
                            animations.Add(name, atlas.GetAnimation(name));
                        }
                    }
                }
                return animations;
            }
        }

        public IEnumerable<ModLinkedAsset> LinkedNodes
        {
            get
            {
                var nodes = new Dictionary<ModNode, ModLinkedAsset>();
                if (RootNode != null)
                {
                    RootNode.GetLinkedNodes(nodes);
                }
                return nodes.Values;
            }
        }

        public IEnumerable<ModLinkedAsset> LinkedSprites
        {
            get
            {
                var sprites = new Dictionary<ModNode, ModLinkedAsset>();
                if (RootNode != null)
                {
                    RootNode.GetLinkedSprites(sprites);
                }
                return sprites.Values;
            }
        }

        public NodePackage()
        {
            atlasList = new List<ModNodeAnimationAtlas>();
        }

        public void AddAtlas(ModNodeAnimationAtlas atlas)
        {
            atlasList.Add(atlas);
        }
    }
}
