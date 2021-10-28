using Cocos2Unity.Models;
using System;
using System.IO;


namespace Cocos2Unity
{
    public static class ProjectConvertor
    {
        public static IParser Parser { get; set; }
        public static IConvertor Convertor { get; set; }

        public static void Convert(string inputPath, string outputPath, bool useProjectNameAsOutputDir = true)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            if (Parser == null)
            {
                throw new Exception("Parser is null");
            }
            if (Convertor == null)
            {
                throw new Exception("Convertor is null");
            }
            Parser.ParseFromPath(inputPath);
            if (useProjectNameAsOutputDir)
            {
                outputPath = Path.Combine(outputPath, Parser.ProjectName);
            }
            Convertor.SetOutputPath(outputPath);

            foreach (var assetpath in Parser.UnparsedAssetPaths)
            {
                Convertor.ImportUnparsedAsset(assetpath, Parser.GetFullAssetPath);
            }
            foreach (var assetpath in Parser.ParsedSpriteLists.Keys)
            {
                Convertor.ConvertSpriteList(assetpath, p => Parser.ParsedSpriteLists[p]);
            }
            foreach (var assetpath in Parser.ParsedNodePackages.Keys)
            {
                Convertor.ConvertNodePackage(assetpath, p => Parser.ParsedNodePackages[p]);
            }
        }
    }
}
