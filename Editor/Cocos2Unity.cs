using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Wynncs.Util;


namespace Cocos2Unity
{
    public class Cocos2Unity<TarConvertor> where TarConvertor : Convertor, new()
    {
        public struct ProjectInfo
        {
            public string projectName;
            public string projectPath;
            public string srcResPath;
            public string expResPath;
            public List<string> csdFiles;
            public List<string> csiFiles;
        }
        public List<ProjectInfo> Projects;
        public string OutFolder;

        public string RelativeSrcResPath { get; set; } = "cocosstudio";
        public string RelativeExpResPath { get; set; } = "res";

        public bool isConvertCSD = true;
        public bool isConvertCSI = true;

        public void Convert(string importPath, string exportPath)
        {
            AnalyseImportPath(importPath);
            AnalyseExportPath(exportPath);
            foreach (var project in Projects)
            {
                ConvertorProject(project);
            }
        }

        private void AnalyseImportPath(string path)
        {
            path = FileSystem.GetPathInfo(path)?.FullName;
            if (path == null)
            {
                throw new Exception("path not find");
            }
            Projects = new List<ProjectInfo>();

            // just part of one project
            var found = false;
            var parentpath = Path.GetDirectoryName(path);
            while (parentpath != null && !found)
            {
                FileSystem.EnumPath(parentpath, f =>
                {
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs" && !found)
                    {
                        var projectInfo = new ProjectInfo();
                        projectInfo.projectPath = Path.GetDirectoryName(f.FullName);
                        projectInfo.projectName = Path.GetFileName(projectInfo.projectPath);
                        projectInfo.srcResPath = projectInfo.projectPath + "\\" + RelativeSrcResPath + "\\";
                        projectInfo.expResPath = projectInfo.projectPath + "\\" + RelativeExpResPath + "\\";
                        CreateImportFileList(ref projectInfo, path.Replace(projectInfo.srcResPath, ""));
                        Projects.Add(projectInfo);
                        found = true;
                    }
                }, false);
                parentpath = Path.GetDirectoryName(parentpath);
            }

            // just part of one project
            if (!found)
            {
                FileSystem.EnumPath(path, f =>
                {
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs")
                    {
                        var projectInfo = new ProjectInfo();
                        projectInfo.projectPath = Path.GetDirectoryName(f.FullName);
                        projectInfo.projectName = Path.GetFileName(projectInfo.projectPath);
                        projectInfo.srcResPath = projectInfo.projectPath + "\\" + RelativeSrcResPath + "\\";
                        projectInfo.expResPath = projectInfo.projectPath + "\\" + RelativeExpResPath + "\\";
                        CreateImportFileList(ref projectInfo);
                        Projects.Add(projectInfo);
                    }
                });
            }
        }

        private void AnalyseExportPath(string path)
        {
            if (TryGetPathFromAsset(path) == null)
            {
                throw new Exception("outpath must in Assets");
            }
            OutFolder = Directory.CreateDirectory(path).FullName;
        }

        private static string TryGetPathFromAsset(string fullpath)
        {
            fullpath = fullpath.Replace('\\', '/');
            var assetPath = Application.dataPath.Replace('\\', '/');
            if (fullpath.Contains(assetPath))
            {
                return fullpath.Replace(assetPath, "Assets");
            }
            return null;
        }

        private void CreateImportFileList(ref ProjectInfo project, string findPath = null)
        {
            var rootPath = project.srcResPath;
                var csdFiles = new List<string>();
                var csiFiles = new List<string>();
            if (findPath == null)
            {
                findPath = project.srcResPath;
            }
            else
            {
                findPath = project.srcResPath + findPath;
            }
            FileSystem.EnumPath(findPath, f =>
            {
                if (!FileSystem.IsFolder(f))
                {
                    if (f.Extension.ToLower() == ".csd" && isConvertCSD)
                    {
                        var csdFileName = f.FullName.Replace(rootPath, "").Replace('\\', '/');
                        csdFiles.Add(csdFileName);
                    }
                    if (f.Extension.ToLower() == ".csi" && isConvertCSI)
                    {
                        var csiFileName = f.FullName.Replace(rootPath, "").Replace('\\', '/');
                        csiFiles.Add(csiFileName);
                    }
                }
            });
            project.csdFiles = csdFiles;
            project.csiFiles = csiFiles;
        }

        private void ConvertorProject(ProjectInfo project)
        {
            Debug.Log($"PROCESS PROJECT >> {project.projectName}");
            TarConvertor convertor = new TarConvertor();
            convertor.SetRootPath(project.srcResPath, new string[] { project.expResPath, });
            convertor.SetMapPath(project.srcResPath, OutFolder + "\\" + project.projectName);
            foreach (var csd in project.csdFiles)
            {
                Debug.Log($"PROCESS CSDFILE  >> {csd}");
                var result = convertor.ConvertCsd(csd, false);
                Debug.Log($"PROCESS CSDFILE  << {csd} - {result}");
            }
            foreach (var csi in project.csiFiles)
            {
                var result = convertor.ConvertCsi(csi, false);
            }
            Debug.Log($"PROCESS PROJECT << {project.projectName}");
            Cocos2Unity.CsdType.SwapAccessLog(OutFolder + "\\" + project.projectName + "_unhandled.xml");
        }
    }

}
