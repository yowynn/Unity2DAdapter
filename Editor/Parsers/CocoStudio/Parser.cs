using System;
using System.Collections.Generic;
using System.IO;
using Unity2DAdapter.Models;
using Unity2DAdapter.Util;

namespace Unity2DAdapter.CocoStudio
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
            GenerateAssetListToParse(path);
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


        public NodePackage ParseCsd(string assetpath)
        {
            NodePackage nodePackage = null;
            try
            {
                var filepath = Path.Combine(SrcResPath, assetpath);
                nodePackage = CsdParser.ParseCsd(filepath);
            }
            catch (Exception e)
            {
                MarkHandleError(assetpath, e.ToString());
            }
            return nodePackage;
        }

        public SpriteList ParseCsi(string assetpath)
        {
            SpriteList spriteList = null;
            try
            {
                var filepath = Path.Combine(SrcResPath, assetpath);
                spriteList = CsiParser.ParseCsi(filepath);
            }
            catch (Exception e)
            {
                MarkHandleError(assetpath, e.ToString());
            }
            return spriteList;
        }

        public bool IsConvertCSD { private get; set; } = true;
        public bool IsConvertCSI { private get; set; } = true;
        public string RelativeSrcResPath { private get; set; } = "cocosstudio";
        public string RelativeExpResPath { private get; set; } = "res";

        private IDictionary<string, bool> dictUnparsedAssetPaths;
        private IDictionary<string, bool> dictErrorAssetPaths;
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

        private void GenerateAssetListToParse(string path)
        {
            csdFiles = new List<string>();
            csiFiles = new List<string>();
            FileSystem.EnumPath(path, f =>
            {
                if (FileSystem.IsFolder(f))
                {
                    return;
                }
                if (f.Extension.ToLower() == ".csd" && IsConvertCSD)
                {
                    var assetpath = f.FullName.Replace(SrcResPath + Path.DirectorySeparatorChar, "").Replace('\\', '/');
                    csdFiles.Add(assetpath);
                }
                else if (f.Extension.ToLower() == ".csi" && IsConvertCSI)
                {
                    var assetpath = f.FullName.Replace(SrcResPath + Path.DirectorySeparatorChar, "").Replace('\\', '/');
                    csiFiles.Add(assetpath);
                }
            }, true);
        }

        private void Parse()
        {
            ProcessLog.Log($"Start Parse CocoStudio project: {ProjectName}");
            ParsedNodePackages = new Dictionary<string, NodePackage>();
            ParsedSpriteLists = new Dictionary<string, SpriteList>();
            dictUnparsedAssetPaths = new Dictionary<string, bool>();
            dictErrorAssetPaths = new Dictionary<string, bool>();
            foreach (var csi in csiFiles)
            {
                // ProcessLog.Log(csi);
                HandleCsiAsset(csi);
            }
            foreach (var csd in csdFiles)
            {
                // ProcessLog.Log(csd);
                HandleCsdAsset(csd);
            }
            ProcessLog.Log($"End Parse CocoStudio project: {ProjectName}");
        }

        private void HandleCsdAsset(string assetpath)
        {
            if (!IsHandled(assetpath))
            {
                ProcessLog.Log($"--Handle CSD Asset: {assetpath}");
                var nodePackage = ParseCsd(assetpath);
                if (nodePackage != null)
                {
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
        }

        private void HandleCsiAsset(string assetpath)
        {
            if (!IsHandled(assetpath))
            {
                ProcessLog.Log($"--Handle CSI Asset: {assetpath}");
                var spriteList = ParseCsi(Path.Combine(SrcResPath, assetpath));
                if (spriteList != null)
                {
                    ParsedSpriteLists.Add(assetpath, spriteList);
                    foreach (var linkedSprite in spriteList.LinkedSprites)
                    {
                        HandleSpriteAsset(linkedSprite.Name);
                    }
                }
            }
        }

        private void HandleSpriteAsset(string assetpath)
        {
            if (!IsHandled(assetpath))
            {
                ProcessLog.Log($"--Handle IMG Asset: {assetpath}");
                dictUnparsedAssetPaths.Add(assetpath, true);
            }
        }

        private void MarkHandleError(string assetpath, string error)
        {
            ProcessLog.Log($"!!Handle Error: {assetpath}");
            ProcessLog.Log(error);
            dictErrorAssetPaths.Add(assetpath, true);
        }

        public bool IsHandled(string assetpath)
        {
            return ParsedNodePackages.ContainsKey(assetpath)
                || ParsedSpriteLists.ContainsKey(assetpath)
                || dictUnparsedAssetPaths.ContainsKey(assetpath)
                || dictErrorAssetPaths.ContainsKey(assetpath);
        }

    }
}
