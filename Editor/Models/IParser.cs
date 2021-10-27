using System.Collections.Generic;

namespace Cocos2Unity.Models
{
    public interface IParser
    {
        string ProjectName{ get; }
        IDictionary<string, NodePackage> ParsedNodePackages{ get; }
        IDictionary<string, SpriteList> ParsedSpriteLists{ get; }
        IEnumerable<string> UnparsedAssetPaths{ get; }
        void ParseFromPath(string path);
        string GetFullAssetPath(string assetpath);
    }
}
