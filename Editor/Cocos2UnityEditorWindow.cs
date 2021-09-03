using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wynnsharp;
using System.IO;

public class Cocos2UnityEditorWindow : EditorWindow
{
    [MenuItem("Window/UIElements/Cocos2UnityEditorWindow")]
    [MenuItem("COCOS/test")]
    public static void ShowExample()
    {
        Cocos2UnityEditorWindow wnd = GetWindow<Cocos2UnityEditorWindow>();
        wnd.titleContent = new GUIContent("Cocos2UnityEditorWindow");
    }

    public string path = @"C:\Users\Wynn\Desktop\book\story_0036\cocosstudio\scenes\story\0036\content\layout\page_1\s0036_h001_siren.csd";
    public string outpath = "./outputFFFFFFF";

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement labelIn = new Label(path);
        root.Add(labelIn);
        VisualElement labelOut = new Label(outpath);
        root.Add(labelOut);
        Button butt = new Button(Test);
        butt.text = "test";
        root.Add(butt);
    }

    public void Test()
    {
        var fs = new Wynnsharp.FileSystemUtil();
        Debug.Log(fs.GetPath(path));
        fs.EnumFolder(path, f =>
        {
            if (!fs.IsFolder(f) && f.Extension.ToLower() == ".csd")
            {
                var csd = fs.AsFile(f);
                Debug.Log(csd.FullName);
            }
        });

    }
}
