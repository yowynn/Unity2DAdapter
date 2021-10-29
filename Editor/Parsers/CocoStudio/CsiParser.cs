using System.Xml;
using Cocos2Unity.Models;
using Cocos2Unity.Util;

namespace Cocos2Unity.CocoStudio
{
    public static class CsiParser
    {
        public static SpriteList ParseCsi(string filepath)
        {
            var TARGET = new SpriteList();
            var file = new XmlDocument();
            file.Load(filepath);
            var root = file.DocumentElement;

            var PropertyGroup = root.GetElement("PropertyGroup");
            var Name = PropertyGroup.GetStringAttribute("Name");
            TARGET.Name = Name;

            var Content = root.GetElement("Content");
            var PicturePadding = Content.GetIntegerAttribute("PicturePadding");
            var AllowRotation = Content.GetBoolAttribute("AllowRotation");
            TARGET.SpritePadding = PicturePadding;
            TARGET.AllowRotation = AllowRotation;

            var ImageFiles = Content.GetElement("ImageFiles");
            foreach (var o in ImageFiles)
            {
                var FilePathData = o as XmlElement;
                if (FilePathData != null)
                {
                    var name = FilePathData.GetStringAttribute("Path");
                    TARGET.AddSpriteInfo(new ModLinkedAsset(name));
                }
            }

            var MaxSize = Content.GetElement("MaxSize");
            var Width = MaxSize.GetIntegerAttribute("Width");
            var Height = MaxSize.GetIntegerAttribute("Height");
            TARGET.MaxTextureSize = new ModVector2(Width, Height);

            return TARGET;
        }
    }
}
