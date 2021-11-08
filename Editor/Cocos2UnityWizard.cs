using System.Collections.Generic;
using System.IO;
using Unity2DAdapter.Util;
using UnityEditor;
using UnityEngine;

namespace Unity2DAdapter
{
    public class Csd2UnityPrefab : ScriptableWizard
    {
        [SerializeField, Tooltip("The FULL Path Import From - can be a file path or a project folder path")]
        public string InputPath = @"C:/Users/Wynn/Desktop/book/story_0037";

        [SerializeField, Tooltip("The FULL Path Export To - must under \"pathtoproject/Assets\"")]
        public string OutputPath = "";

        [SerializeField, Tooltip("The RELATIVE Path To Find COCOS Source File (RELATIVE to COCOS Project)")]
        public string RelativeSrcResPath = "cocosstudio";

        [SerializeField, Tooltip("The RELATIVE Path To Find COCOS Export File (RELATIVE to COCOS Project)")]
        public string RelativeExpResPath = "res";

        [SerializeField, Tooltip("use the project name as an additional parent path")]
        public bool UseProjectNameAsParentPath = false;

        private static string DefaultOutPath => Application.dataPath + "/art/story";

        [MenuItem("ToUnity2D/ConventCocoStudioProjects")]
        static void CreateWizard()
        {
            var wzd = ScriptableWizard.DisplayWizard<Csd2UnityPrefab>("Convent CocoStudio Projects to Unity Animated Canvas Prefab", "Don't Click!", "Apply");
            wzd.OutputPath = DefaultOutPath;
        }

        void OnWizardCreate()
        {
            // Create();
            Test();
        }

        void OnWizardUpdate()
        {
            helpString = @"Convert COCOS `.csd` to UNITY `.prefab`
            Convert COCOS `.plist` to UNITY multi-sprite";
            InputPath = InputPath.Replace('\\', '/');
            OutputPath = OutputPath.Replace('\\', '/');
        }

        // When the user presses the "Apply" button OnWizardOtherButton is called.
        void OnWizardOtherButton()
        {
            // XmlAnalyze(@"C:\Users\Wynn\Desktop\book", "out/csd.xml", ".csd");
            // XmlAnalyze(@"C:\Users\Wynn\Desktop\book", "out/plist.xml", ".plist");
            // LoadPlist(@"P:\Gitlab\ihuman-ams\project-dev\cocos-demo\Assets\output\s0036_h001_siren_0.plist");
            ConventCocoStudioProjects();
        }

        public static void XmlAnalyze(string path, string outfile, string extention = null)
        {
            path = "C:/Users/Wynn/Desktop/book";
            FileSystem.EnumPath(path, f =>
            {
                if (!FileSystem.IsFolder(f) && (extention == null || f.Extension.ToLower() == extention))
                {
                    var srcfile = f.FullName;
                    XmlUtil.Statistics(srcfile, outfile, true);
                }
            });
        }

        public void ConventCocoStudioProjects()
        {
            ProjectConvertor.Parser = new CocoStudio.Parser
            {
                RelativeSrcResPath = RelativeSrcResPath,
                RelativeExpResPath = RelativeExpResPath,
                IsConvertCSD = true,
                IsConvertCSI = true,
            };
            ProjectConvertor.Convertor = new Unity.CanvasAnimatedGameObjectConvertor();

            var projects = EnumCocoStudioProjects(InputPath);
            foreach (var project in projects)
            {
                ProjectConvertor.Convert(project.FindPath, OutputPath, UseProjectNameAsParentPath);
            }
        }

        private struct CocoStudioProjectInfo
        {
            public string Name;
            public string Path;
            public string FindPath;
        }

        private static CocoStudioProjectInfo[] EnumCocoStudioProjects(string path)
        {
            var list = new List<CocoStudioProjectInfo>();

            // just part of one project
            var found = false;
            var parentpath = Path.GetDirectoryName(path);
            while (parentpath != null && !found)
            {
                FileSystem.EnumPath(parentpath, f =>
                {
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs" && !found)
                    {
                        CocoStudioProjectInfo info = new CocoStudioProjectInfo();
                        info.Path = Path.GetDirectoryName(f.FullName);
                        info.Name = Path.GetFileName(info.Path);
                        info.FindPath = path;
                        list.Add(info);
                        found = true;
                    }
                }, false);
                parentpath = Path.GetDirectoryName(parentpath);
            }

            if (!found)
            {
                FileSystem.EnumPath(path, f =>
                {
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".ccs")
                    {
                        CocoStudioProjectInfo info = new CocoStudioProjectInfo();
                        info.Path = Path.GetDirectoryName(f.FullName);
                        info.Name = Path.GetFileName(info.Path);
                        info.FindPath = info.Path;
                        list.Add(info);
                    }
                });
            }
            return list.ToArray();
        }

        static void Test()
        {
        }
    }
}
