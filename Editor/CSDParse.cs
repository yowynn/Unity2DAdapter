using System;
using System.Xml;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.IO;

public class CSDParse
{
    public class ObjectDataParse
    {
        public delegate void OnProperty(GameObject go, XmlElement prop);
        public static Dictionary<string, OnProperty> OnPropertyMap;

        public static System.Random Random = new System.Random();

        private void BuildOnPropertyMap()
        {
            OnPropertyMap = new Dictionary<string, OnProperty>();
            OnPropertyMap.Add("Children", (go, prop) =>
            {
                foreach(XmlElement node in prop)
                {
                    if (node.Name == "AbstractNodeData")
                    {
                        // Debug.Log(node.Attributes["Name"].Value);
                        var child = ParseAbstractNode(node);
                        child.transform.SetParent(go.transform);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            });
            OnPropertyMap.Add("Position", (go, prop) =>
            {
                float x = float.Parse(prop.Attributes["X"]?.Value ?? "0");
                float y = float.Parse(prop.Attributes["Y"]?.Value ?? "0");
                go.transform.localPosition = new Vector3(x, y, 0);
            });
            OnPropertyMap.Add("Size", (go, prop) =>
            {
                float x = float.Parse(prop.Attributes["X"]?.Value ?? "0");
                float y = float.Parse(prop.Attributes["Y"]?.Value ?? "0");
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(x, y);
            });
            OnPropertyMap.Add("FileData", (go, prop) =>
            {
                var image = go.AddComponent<Image>();
                image.color = new Color(Random.Next(100) / 100f, Random.Next(100) / 100f, Random.Next(100) / 100f);

                var path = prop.Attributes["Path"]?.Value;
                var plist = prop.Attributes["Plist"]?.Value;
                holder.ImportFileData(path, plist);
            });
        }
        public CSDParse holder;
        public ObjectDataParse(CSDParse holder)
        {
            this.holder = holder;
            BuildOnPropertyMap();
        }
        public void Parse(XmlElement node)
        {
            var go = ParseAbstractNode(node);
            var s = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>()[0];
            go.transform.SetParent(s.transform);
        }

        private static GameObject ParseAbstractNode(XmlNode node)
        {
            var name = node.Name;

            var go = new GameObject(node.Attributes["Name"].Value);
            go.AddComponent<RectTransform>();
            foreach (XmlElement prop in node)
            {
                OnProperty onProperty;
                if (OnPropertyMap.TryGetValue(prop.Name, out onProperty))
                {
                    onProperty(go, prop);
                }
                else
                {

                }
            }
            return go;
        }
    }


    private string projectPath;
    private List<string> resPath;
    private string srcFile;
    private string tarPath;
    private XmlDocument root;
    public XmlElement ObjectData;

    public CSDParse()
    {

    }

    public void SetPath(string projectPath, string[] resPath)
    {
        if (Directory.Exists(projectPath))
        {
            projectPath = projectPath.Replace('\\', '/');
            projectPath += projectPath.EndsWith("/") ? "" : "/";
            this.projectPath = projectPath;

            this.resPath = new List<string>();
            this.resPath.Add(projectPath);
            foreach (string path in resPath)
            {
                if (Directory.Exists(path))
                {
                    string p = path.Replace('\\', '/');
                    p += p.EndsWith("/") ? "" : "/";
                    if (!this.resPath.Contains(p))
                    {
                        this.resPath.Add(p);
                    }
                }
            }
        }
        else
        {
            this.projectPath = null;
            this.resPath = null;
        }
    }

    public void SetTrans(string srcFile, string tarPath)
    {
        srcFile = srcFile.Replace('\\', '/');
        if (projectPath != null && Path.GetExtension(srcFile).ToLower() == ".csd")
        {
            if (File.Exists(srcFile))
            {
                this.srcFile = srcFile;
            }
            else if (File.Exists(projectPath + srcFile))
            {
                this.srcFile = projectPath + srcFile;
            }
            else
            {
                throw new Exception();
            }
        }
        else
        {
            throw new Exception();
        }
        this.tarPath = Directory.CreateDirectory(tarPath).FullName.Replace('\\', '/') + "/";
        this.root = new Wynnsharp.XmlUtil().OpenXml(this.srcFile);
        if (this.root == null)
        {
            throw new Exception();
        }
    }


    public void PreParse()
    {
        var GameProjectFile = root["GameProjectFile"];
        if (GameProjectFile != null)
        {
            var PropertyGroup = GameProjectFile["PropertyGroup"];
            if (PropertyGroup != null)
            {

            }
            else
            {
                throw new Exception("PropertyGroup");
            }
            var Content = GameProjectFile["Content"]?["Content"];
            if (Content != null)
            {
                var ObjectData = Content["ObjectData"];
                if (ObjectData != null)
                {
                    this.ObjectData = ObjectData;
                }
            }
            else
            {
                throw new Exception("Content");
            }
        }
        else
        {
            throw new Exception("GameProjectFile");
        }
    }

    public void Parse(string csdFile, string outFolder)
    {
        SetTrans(csdFile, outFolder);
        PreParse();
        if (ObjectData != null)
        {
            var parse = new ObjectDataParse(this);
            parse.Parse(ObjectData);
        }
        Test();
    }

    public string TryFindFilePath(string tarPath)
    {
        foreach (var path in resPath)
        {
            if (File.Exists(path + tarPath))
            {
                return path + tarPath;
            }
        }
        return null;
    }

    public void ImportFileData(string path, string plist)
    {
        var png = TryFindFilePath(Path.ChangeExtension(plist, ".png"));
        path = TryFindFilePath(path);
        plist = TryFindFilePath(plist);
        Debug.Log(path);
        Debug.Log(plist);
        Debug.Log(png);
        if (plist != null && png != null)
        {
            File.Copy(plist, tarPath + Path.GetFileName(plist), true);
            File.Copy(png, tarPath + Path.GetFileName(png), true);
        }
    }

    public void Test()
    {
        Debug.Log("Succ");
    }

}
