using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text.RegularExpressions;

namespace Cocos2Unity
{
    public class Convertor
    {
        public static System.Random Random = new System.Random();
        private string ProjectPath;
        private List<string> ResPath;
        private string CsdFile;
        private string OutFolder;
        private CsdParser parser;

        public void SetPath(string projectPath, string[] resPath)
        {
            if (Directory.Exists(projectPath))
            {
                projectPath = projectPath.Replace('\\', '/');
                projectPath += projectPath.EndsWith("/") ? "" : "/";
                ProjectPath = projectPath;

                ResPath = new List<string>();
                ResPath.Add(projectPath);
                foreach (string path in resPath)
                {
                    if (Directory.Exists(path))
                    {
                        string p = path.Replace('\\', '/');
                        p += p.EndsWith("/") ? "" : "/";
                        if (!ResPath.Contains(p))
                        {
                            ResPath.Add(p);
                        }
                    }
                }
            }
            else
            {
                ProjectPath = null;
                ResPath = null;
            }
        }

        public void SetTransTo(string csdFile, string outFolder)
        {
            csdFile = csdFile.Replace('\\', '/');
            if (Path.GetExtension(csdFile).ToLower() == ".csd")
            {
                if (File.Exists(csdFile))
                {
                    CsdFile = csdFile;
                }
                else if (ProjectPath != null && File.Exists(ProjectPath + csdFile))
                {
                    CsdFile = ProjectPath + csdFile;
                }
                else
                {
                    throw new Exception("no csd file found");
                }
            }
            else
            {
                throw new Exception("not a csd file");
            }
            OutFolder = Directory.CreateDirectory(outFolder).FullName.Replace('\\', '/') + "/";
        }

        public void Convert(string csdFile, string outFolder)
        {
            SetTransTo(csdFile, outFolder);
            var xml = new Wynnsharp.XmlUtil().OpenXml(CsdFile);
            parser = new CsdParser(xml);
            var root = ConvertGameObject(parser.Node);
            var s = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>()[0];
            root.transform.SetParent(s.transform, false);
        }

        private GameObject ConvertGameObject(CsdNode node)
        {
            var go = new GameObject(node.Name);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(node.Size.X, node.Size.Y);
            rt.localPosition = new Vector3(node.Position.X, node.Position.Y, 0);
            if (node.Image != null)
            {
                var image = go.AddComponent<Image>();
                image.color = new Color(Random.Next(100) / 100f, Random.Next(100) / 100f, Random.Next(100) / 100f);
                LoadImage(node.Image);
            }
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var go0 = ConvertGameObject(child);
                    go0.transform.SetParent(rt, false);
                }
            }
            return go;
        }

        private string TryGetFullResPath(string path)
        {
            foreach (var p in ResPath)
            {
                if (File.Exists(p + path))
                {
                    return p + path;
                }
            }
            return null;
        }
        private string TryGetFullOutPath(string path)
        {
            var outpath = OutFolder + Path.GetFileName(path);
            return outpath;
        }

        private string TryGetPathFromAsset(string path)
        {
            path = path.Replace('\\', '/');
            var assetPath = Application.dataPath.Replace('\\', '/');
            if (path.Contains(assetPath))
            {
                return path.Replace(assetPath, "Assets");
            }
            return null;
        }
        private void ConvertPlist(string plist, string mergeimg)
        {
            mergeimg = TryGetPathFromAsset(mergeimg);
            AssetDatabase.ImportAsset(mergeimg);
            var pd = new PlistDocument();
            pd.ReadFromFile(plist);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(mergeimg);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            var frames = pd.root["frames"].AsDict().values;
            var spritesheet = new SpriteMetaData[frames.Count];
            int idx = 0;
            var size = StringToVector2(pd.root["metadata"].AsDict().values["size"].AsString());
            foreach(var frame in frames)
            {
                var name = frame.Key;
                var props = frame.Value.AsDict().values;
                var md = new SpriteMetaData();
                md.name = Path.GetFileName(name);
                Rect rect = default;
                bool rotated = false;
                foreach (var prop in props)
                {
                    switch (prop.Key)
                    {
                        case "frame":
                            {
                                var val = StringToRect(prop.Value.AsString());
                                rect = val;
                                break;
                            }
                        case "offset":
                            {
                                var val = StringToVector2(prop.Value.AsString());
                                break;
                            }
                        case "rotated":
                            {
                                var val = prop.Value.AsBoolean();
                                rotated = val;
                                break;
                            }
                        case "sourceSize":
                            {
                                var val = StringToVector2(prop.Value.AsString());
                                break;
                            }
                        default:
                            {
                                Debug.Log($"{prop.Key} not handled");
                                break;
                            }
                    }
                }
                if (rotated)
                {
                    var h = rect.height;
                    rect.height = rect.width;
                    rect.width = h;
                }
                rect.y = size.y - rect.y - rect.height;

                md.rect = rect;
                spritesheet[idx++] = md;
            }
            importer.spritesheet = spritesheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
        private void LoadImage(CsdFile image)
        {
            var Plist = TryGetFullResPath(image.Plist);
            var Res = TryGetFullResPath(image.Path);
            var Merge = TryGetFullResPath(Path.ChangeExtension(image.Plist, ".png"));
            if (Merge != null)
            {
                var OutMerge = TryGetFullOutPath(Merge);
                if (!File.Exists(OutMerge))
                {
                    File.Copy(Merge, OutMerge);
                    Debug.Log(File.Exists(OutMerge));
                    ConvertPlist(Plist, OutMerge);
                }
            }
        }

        private static Rect StringToRect(string val)
        {
            var nums = val.Replace("{", "").Replace("}", "").Split(',');
            return new Rect(int.Parse(nums[0]), int.Parse(nums[1]), int.Parse(nums[2]), int.Parse(nums[3]));
        }

        private static Vector2 StringToVector2(string val)
        {
            var nums = val.Replace("{", "").Replace("}", "").Split(',');
            return new Vector2(int.Parse(nums[0]), int.Parse(nums[1]));
        }

    }
}
