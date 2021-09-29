using UnityEditor;
using UnityEngine;
using System.IO;
using Wynncs.Util;

namespace Cocos2Unity
{
public class Csd2UnityPrefab : ScriptableWizard
    {
        public string InForder = @"C:\Users\Wynn\Desktop\book\story_0036\cocosstudio\scenes\story\0036\content\layout\page_1\s0036_h001_siren.csd";
        public string OutFolder = "";

        public static string DefaultOutPath => Application.dataPath + "/art/story";

        [MenuItem("COCOS/Csd2UnityPrefab")]
        static void CreateWizard()
        {
            var wzd = ScriptableWizard.DisplayWizard<Csd2UnityPrefab>("Create Prefab", "Create", "Apply");
            wzd.OutFolder = DefaultOutPath;
        }

        void OnWizardCreate()
        {
            // Create();
        }

        void OnWizardUpdate()
        {
            helpString = "Please set the color of the light!";
            InForder = InForder.Replace('\\', '/');
            OutFolder = OutFolder.Replace('\\', '/');
        }

        // When the user presses the "Apply" button OnWizardOtherButton is called.
        void OnWizardOtherButton()
        {
            // XmlAnalyze(@"C:\Users\Wynn\Desktop\book", "out/csd.xml", ".csd");
            // XmlAnalyze(@"C:\Users\Wynn\Desktop\book", "out/plist.xml", ".plist");
            // LoadPlist(@"P:\Gitlab\ihuman-ams\project-dev\cocos-demo\Assets\output\s0036_h001_siren_0.plist");
            // Create();
            ConventCsds();
        }

        public static string TryGetRootPath(string fullpath, out string rootPath, out string[] extraPath)
        {
            rootPath = null;
            extraPath = null;
            fullpath = fullpath.Replace('\\', '/');
            if (fullpath.Contains("cocosstudio"))
            {
                string projectPath = fullpath.Substring(0, fullpath.LastIndexOf("cocosstudio"));
                if (Directory.Exists(projectPath + "cocosstudio"))
                {
                    rootPath = projectPath + "cocosstudio/";
                }
                if (Directory.Exists(projectPath + "res"))
                {
                    extraPath = new string[] { projectPath + "res/" };
                }
                return fullpath.Replace(rootPath ?? "", "");
            }
            else
            {
                throw new System.Exception();
            }
        }

        public static void XmlAnalyze(string path, string outfile, string extention = null)
        {
            path = "C:/Users/Wynn/Desktop/book";
            FileSystem.EnumPath(path, f =>
            {
                if (!FileSystem.IsFolder(f) && (extention == null || f.Extension.ToLower() == extention))
                {
                    var srcfile = f.FullName;
                    // Debug.Log(srcfile);
                    XmlUtil.Statistics(srcfile, outfile, true);
                }
            });
        }

        public void ConventCsds()
        {
            new Cocos2Unity.Convertor.ConvertorProjects<Cocos2Unity.UINodeConvertor>().Convert(InForder, OutFolder);
        }
    }
}
