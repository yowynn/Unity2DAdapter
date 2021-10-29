using System;
using System.IO;
using Cocos2Unity.Models;
using Cocos2Unity.Util;


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
                try
                {
                    Convertor.ImportUnparsedAsset(assetpath, Parser.GetFullAssetPath);
                }
                catch (Exception e)
                {
                    ProcessLog.LogError($"Failed to import asset {assetpath}, {e.Message}");
                }
            }
            foreach (var assetpath in Parser.ParsedSpriteLists.Keys)
            {
                try
                {
                    Convertor.ConvertSpriteList(assetpath, p => Parser.ParsedSpriteLists[p]);
                }
                catch (Exception e)
                {
                    ProcessLog.LogError($"Failed to import asset {assetpath}, {e.Message}");
                }
            }
            foreach (var assetpath in Parser.ParsedNodePackages.Keys)
            {
                try
                {
                    Convertor.ConvertNodePackage(assetpath, p => Parser.ParsedNodePackages[p]);
                }
                catch (Exception e)
                {
                    ProcessLog.LogError($"Failed to import asset {assetpath}, {e.Message}");
                }
            }

            ProcessLog.Flush(Path.Combine(outputPath, "process.log"));
            XmlAnalyzer.Flush(Path.Combine(outputPath, "analyser.xml"));
        }
    }
}
