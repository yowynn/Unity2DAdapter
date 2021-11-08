using System;

namespace Unity2DAdapter.Models
{
    public interface IConvertor
    {
        void SetOutputPath(string path);
        void ImportUnparsedAsset(string assetpath, Func<string, string> GetFullPath);
        void ConvertSpriteList(string assetpath, Func<string, SpriteList> GetSpriteList);
        void ConvertNodePackage(string assetpath, Func<string, NodePackage> GetNodePackage);
    }
}
