using UnityEditor;
using UnityEngine;
using System.IO;
using Wynncs.Util;

namespace Cocos2Unity
{
    public class Csd2UnityPrefab : ScriptableWizard
    {
        [SerializeField, Tooltip("The FULL Path Import From - can be a file path or a project folder path")]
        public string InputPath = @"C:\Users\Wynn\Desktop\book\story_0036\cocosstudio\scenes\story\0036\content\layout\page_1\s0036_h001_siren.csd";

        [SerializeField, Tooltip("The FULL Path Export To - must under \"pathtoproject/Assets\"")]
        public string OutputPath = "";

        [SerializeField, Tooltip("The RELATIVE Path To Find COCOS Source File (RELATIVE to COCOS Project)")]
        public string RelativeSrcResPath = "cocosstudio";

        [SerializeField, Tooltip("The RELATIVE Path To Find COCOS Export File (RELATIVE to COCOS Project)")]

        public string RelativeExpResPath = "res";

        private static string DefaultOutPath => Application.dataPath + "/art/story";

        [MenuItem("COCOS/Csd2UnityPrefab")]
        static void CreateWizard()
        {
            var wzd = ScriptableWizard.DisplayWizard<Csd2UnityPrefab>("Create Prefab", "Create", "Apply");
            wzd.OutputPath = DefaultOutPath;
        }

        void OnWizardCreate()
        {
            // Create();
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
            // Create();
            ConventCsds();
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
            var pc = new Cocos2Unity<UINodeConvertor>();
            pc.RelativeSrcResPath = RelativeSrcResPath;
            pc.RelativeExpResPath = RelativeExpResPath;
            pc.isConvertCSD = true;
            pc.isConvertCSI = true;
            pc.Convert(InputPath, OutputPath);
        }
    }
}
