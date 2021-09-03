using UnityEditor;
using UnityEngine;
using System.IO;
using Cocos2Unity;

public class Csd2UnityPrefab : ScriptableWizard
{
    public string CSDPath = @"C:\Users\Wynn\Desktop\book\story_0036\cocosstudio\scenes\story\0036\content\layout\page_1\s0036_h001_siren.csd";
    public string OutPath = "";

    public static string DefaultOutPath => Application.dataPath + "/output";

    [MenuItem("COCOS/Csd2UnityPrefab")]
    static void CreateWizard()
    {
        var wzd = ScriptableWizard.DisplayWizard<Csd2UnityPrefab>("Create Prefab", "Create", "Apply");
        wzd.OutPath = DefaultOutPath;
    }

    void OnWizardCreate()
    {
        // Create();
    }

    void OnWizardUpdate()
    {
        helpString = "Please set the color of the light!";
        CSDPath = CSDPath.Replace('\\', '/');
        OutPath = OutPath.Replace('\\', '/');
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
        var fs = new Wynnsharp.FileSystemUtil();
        var xml = new Wynnsharp.XmlUtil();

        path = "C:/Users/Wynn/Desktop/book";
        fs.EnumFolder(path, f =>
        {
            if (!fs.IsFolder(f) && (extention == null || f.Extension.ToLower() == extention))
            {
                var srcfile = f.FullName;
                // Debug.Log(srcfile);
                xml.Statistics(srcfile, outfile, true);
            }
        });
    }

    public static void LoadPlist(string path)
    {
        var pd = new Cocos2Unity.PlistDocument();
        pd.ReadFromFile(path);
        Debug.Log(pd);
        // Metadata metadata = CreateMetadata(pd.root["metadata"].AsDict());
        // List<Frame> frames = new List<Frame>();
        // foreach (var kvPair in pd.root["frames"].AsDict().values)
        // {
        //     frames.Add(CreateFrame(kvPair.Key, kvPair.Value.AsDict()));
        // }
        // return new Plist(pd.version, metadata, frames);
    }

    public void ConventCsd(string rootPath, string csdPath, string[] extraPath, string outpath)
    {
        var convertor = new Cocos2Unity.Convertor();
        convertor.SetPath(rootPath, extraPath);
        convertor.Convert(csdPath, outpath);
    }

    public void ConventCsds()
    {
        var fs = new Wynnsharp.FileSystemUtil();
        var xml = new Wynnsharp.XmlUtil();
        var relativePath = fs.GetPath(CSDPath).Replace('\\', '/');
        Directory.CreateDirectory(OutPath);

        fs.EnumFolder(CSDPath, f =>
        {
            if (!fs.IsFolder(f) && f.Extension.ToLower() == ".csd")
            {
                var csd = fs.AsFile(f);
                string fullName = csd.FullName.Replace('\\', '/');
                string rootPath;
                string[] extraPath;
                string csdPath = TryGetRootPath(fullName, out rootPath, out extraPath);
                string outpath = fs.GetPath(fullName).Replace('\\', '/').Replace(relativePath, OutPath);
                ConventCsd(rootPath, csdPath, extraPath, outpath);
            }
        });
    }
}
