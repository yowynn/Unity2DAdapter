using System.Collections.Generic;

namespace Cocos2Unity.Models
{
    public class SpriteList
    {
        public ModString Name;
        public ModVector2 MaxTextureSize;
        public ModBoolean AllowRotation;
        public ModInt32 SpritePadding;
        public List<ModLink> SpriteInfos;

        public SpriteList()
        {
            Name = "";
            MaxTextureSize = new ModVector2(0, 0);
            AllowRotation = false;
            SpritePadding = 0;
            SpriteInfos = new List<ModLink>();
        }

        public void AddSpriteInfo(ModLink spriteInfo)
        {
            SpriteInfos.Add(spriteInfo);
        }
    }
}
