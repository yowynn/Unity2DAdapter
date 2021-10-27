using Cocos2Unity.Models;
using System;
using System.IO;
using System.Collections.Generic;
using Wynncs.Util;

namespace Cocos2Unity.CocoStudio
{
    public partial class Parser : IParser
    {
        # region Implemented IParser
        public string ProjectName { get; private set; }
        public IDictionary<string, NodePackage> ParsedNodePackages { get; private set; }
        public IDictionary<string, SpriteList> ParsedSpriteLists { get; private set; }
        public IEnumerable<string> UnparsedAssetPaths => dictUnparsedAssetPaths?.Keys;

        public void ParseFromPath(string path)
        {
            ParseProjectPath(path);
            GenerateAssetListToParse();
            Parse();
        }

        public string GetFullAssetPath(string assetpath)
        {
            if (!string.IsNullOrEmpty(SrcResPath) && File.Exists(Path.Combine(SrcResPath, assetpath)))
                return Path.Combine(SrcResPath, assetpath);
            else if (!string.IsNullOrEmpty(ExpResPath) && File.Exists(Path.Combine(ExpResPath, assetpath)))
                return Path.Combine(ExpResPath, assetpath);
            else if (!string.IsNullOrEmpty(ProjectPath) && File.Exists(Path.Combine(ProjectPath, assetpath)))
                return Path.Combine(ProjectPath, assetpath);
            else
                return null;
        }

        # endregion


        public static partial NodePackage ParseCsd(string filepath);
        public static partial SpriteList ParseCsi(string filepath);

        public bool IsConvertCSD { private get; set; } = true;
        public bool IsConvertCSI { private get; set; } = true;
        public string RelativeSrcResPath { private get; set; } = "cocosstudio";
        public string RelativeExpResPath { private get; set; } = "res";

        private IDictionary<string, bool> dictUnparsedAssetPaths;
        public string ProjectPath { get; private set; }
        public string SrcResPath { get; private set; }
        public string ExpResPath { get; private set; }
        private List<string> csdFiles;
        private List<string> csiFiles;

        private void ParseProjectPath(string path)
        {
            path = FileSystem.GetPathInfo(path)?.FullName;
            if (path == null)
            {
                throw new Exception("path not find");
            }
            string RootPath = null;

            // just part of one project
            var parentpath = Path.GetDirectoryName(path);
            while (parentpath != null)
            {
                FileSystem.EnumPath(parentpath, f =>
                {
                    if (RootPath == null && !FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs")
                    {
                        RootPath = Path.GetDirectoryName(f.FullName);
                    }
                }, false);
                if (RootPath != null)
                {
                    break;
                }
                parentpath = Path.GetDirectoryName(parentpath);
            }

            // find in sub folders
            if (RootPath == null)
            {
                FileSystem.EnumPath(path, f =>
                {
                    if (RootPath == null && !FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs")
                    {
                        RootPath = Path.GetDirectoryName(f.FullName);
                    }
                }, true);
            }

            if (RootPath == null)
            {
                throw new Exception("can not find project root path");
            }
            ProjectPath = RootPath;
            ProjectName = Path.GetFileName(ProjectPath);
            if (!string.IsNullOrEmpty(RelativeSrcResPath))
            {
                SrcResPath = Path.Combine(ProjectPath, RelativeSrcResPath);
            }
            else
            {
                SrcResPath = ProjectPath;
            }
            if (!string.IsNullOrEmpty(RelativeExpResPath))
            {
                ExpResPath = Path.Combine(ProjectPath, RelativeExpResPath);
            }
            else
            {
                ExpResPath = null;
            }
        }

        private void GenerateAssetListToParse()
        {
            csdFiles = new List<string>();
            csiFiles = new List<string>();
            FileSystem.EnumPath(SrcResPath, f =>
            {
                if (FileSystem.IsFolder(f))
                {
                    return;
                }
                if (f.Extension.ToLower() == ".csd" && IsConvertCSD)
                {
                    var assetpath = f.FullName.Replace(SrcResPath + Path.PathSeparator, "").Replace('\\', '/');
                    csdFiles.Add(assetpath);
                }
                else if (f.Extension.ToLower() == ".csi" && IsConvertCSI)
                {
                    var assetpath = f.FullName.Replace(SrcResPath + Path.PathSeparator, "").Replace('\\', '/');
                    csiFiles.Add(assetpath);
                }
            }, true);
        }

        private void Parse()
        {
            ProsessLog.Log($"Start Parse CocoStudio project: {ProjectName}");
            ParsedNodePackages = new Dictionary<string, NodePackage>();
            ParsedSpriteLists = new Dictionary<string, SpriteList>();
            dictUnparsedAssetPaths = new Dictionary<string, bool>();
            foreach (var csi in csiFiles)
            {
                HandleCsiAsset(csi);
            }
            foreach (var csd in csdFiles)
            {
                HandleCsdAsset(csd);
            }
            ProsessLog.Log($"End Parse CocoStudio project: {ProjectName}");
        }

        private void HandleCsdAsset(string assetpath)
        {
            if (!ParsedNodePackages.ContainsKey(assetpath))
            {
                ProsessLog.Log($"--Handle Csd Asset: {assetpath}");
                var nodePackage = ParseCsd(Path.Combine(SrcResPath, assetpath));
                ParsedNodePackages.Add(assetpath, nodePackage);
                foreach (var linkedNode in nodePackage.LinkedNodes)
                {
                    HandleCsdAsset(linkedNode.Name);
                }
                foreach (var linkedSprite in nodePackage.LinkedSprites)
                {
                    HandleSpriteAsset(linkedSprite.Name);
                }

            }
        }

        private void HandleCsiAsset(string assetpath)
        {
            if (!ParsedSpriteLists.ContainsKey(assetpath))
            {
                ProsessLog.Log($"--Handle Csi Asset: {assetpath}");
                var spriteList = ParseCsi(Path.Combine(SrcResPath, assetpath));
                ParsedSpriteLists.Add(assetpath, spriteList);
                foreach (var linkedSprite in spriteList.LinkedSprites)
                {
                    HandleSpriteAsset(linkedSprite.Name);
                }
            }
        }

        private void HandleSpriteAsset(string assetpath)
        {
            if (!dictUnparsedAssetPaths.ContainsKey(assetpath))
            {
                ProsessLog.Log($"--Handle Sprite Asset: {assetpath}");
                dictUnparsedAssetPaths.Add(assetpath, true);
            }
        }

        public bool IsHandled(string assetpath)
        {
            return ParsedNodePackages.ContainsKey(assetpath) || ParsedSpriteLists.ContainsKey(assetpath) || dictUnparsedAssetPaths.ContainsKey(assetpath);
        }

    }
}