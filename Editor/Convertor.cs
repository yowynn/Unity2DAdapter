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
        public class ConvertedSprite
        {
            public string name;
            public string fullname;
            public Sprite sprite;
            public bool rotated;
        }

        public static System.Random Random = new System.Random();
        public string ProjectPath;
        public List<string> ResPath;
        public string CsdFile;
        public string OutFolder;
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
                if (resPath != null)
                {
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
            var root = ConvertCanvasGameObject(parser.Node);
            var s = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>()[0];
            root.transform.SetParent(s.transform, false);
        }

        private GameObject ConvertCanvasGameObject(CsdNode node, GameObject parent = null)
        {
            // ! do not change the order!!
            var go = new GameObject();
            var rt = go.AddComponent<RectTransform>();
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }
            ConvertCanvasGameObject_Name(go, node.Name);
            ConvertCanvasGameObject_isActive(go, node.isActive);
            ConvertCanvasGameObject_Size(go, node.Size);
            ConvertCanvasGameObject_Position(go, node.Position);
            ConvertCanvasGameObject_Rotation(go, node.Rotation);
            ConvertCanvasGameObject_Scale(go, node.Scale);
            ConvertCanvasGameObject_Pivot(go, node.Pivot);
            ConvertCanvasGameObject_Anchor(go, node.Anchor);
            ConvertCanvasGameObject_Image(go, node.Image);
            ConvertCanvasGameObject_Color(go, node.Color);
            ConvertCanvasGameObject_isInteractive(go, node.isInteractive);
            ConvertCanvasGameObject_BackgroundColor(go, node.BackgroundColor);
            ConvertCanvasGameObject_Children(go, node.Children);
            return go;
        }
        private Action<GameObject, string> ConvertCanvasGameObject_Name = (go, val) => go.name = val;
        private Action<GameObject, bool> ConvertCanvasGameObject_isActive = (go, val) => go.SetActive(val);
        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Size = (go, val) => go.GetComponent<RectTransform>().sizeDelta = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Position = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition3D = new Vector3(val.X, val.Y, val.Z);
        };

        private Action<GameObject, CsdSize> ConvertCanvasGameObject_Rotation = (go, val) => go.GetComponent<RectTransform>().Rotate(val.X, val.Y, val.Z);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Scale = (go, val) => go.GetComponent<RectTransform>().localScale = new Vector3(val.X, val.Y, val.Z);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Pivot = (go, val) => go.GetComponent<RectTransform>().pivot = new Vector2(val.X, val.Y);
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_Anchor = (go, val) =>
        {
            var rt = go.GetComponent<RectTransform>();
            var pos = rt.localPosition;
            var sizeDelta = rt.sizeDelta;
            rt.anchorMin = new Vector2(val.X, val.Y);
            rt.anchorMax = new Vector2(val.X, val.Y);
            rt.localPosition = pos;
            rt.sizeDelta = sizeDelta;
        };
        private Action<GameObject, CsdScale> ConvertCanvasGameObject_AnchorMax = (go, val) => go.GetComponent<RectTransform>().anchorMax = new Vector2(val.X, val.Y);
        private void ConvertCanvasGameObject_Image(GameObject go, CsdFile val) { if (val != null) LoadCanvasImage(go, val); }
        private void ConvertCanvasGameObject_Color(GameObject go, CsdColor val) { if (val != null) SetCanvasImageColor(go, val); }
        private void ConvertCanvasGameObject_isInteractive(GameObject go, bool val) { SetCanvasImageInteractive(go, val); }
        private Action<GameObject, CsdColorGradient> ConvertCanvasGameObject_BackgroundColor = (go, val) => {/* TODO */ };
        private void ConvertCanvasGameObject_Children(GameObject go, List<CsdNode> val) { if (val != null) foreach (var child in val) ConvertCanvasGameObject(child, go); }

        private void LoadCanvasImage(GameObject go, CsdFile imageData)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                image = go.AddComponent<Image>();
            }
            // image.color = new Color(Random.Next(100) / 100f, Random.Next(100) / 100f, Random.Next(100) / 100f);
            var convertedSprite = ImportPlist(imageData.Plist)?[imageData.Path] ?? ImportSprite(imageData.Path);
            if (convertedSprite?.sprite != null)
            {
                image.sprite = convertedSprite.sprite;
                if (convertedSprite.rotated)
                {
                    // image.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    image.transform.Rotate(0, 0, 90);
                    var rt = image.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(rt.sizeDelta.y, rt.sizeDelta.x);
                    rt.pivot = new Vector2(rt.pivot.y, 1 - rt.pivot.x);
                }
            }
        }

        private void SetCanvasImageColor(GameObject go, CsdColor color)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                // image = go.AddComponent<Image>();
                return;
            }
            image.color = new Color(color.R, color.G, color.B, color.A);
        }

        private void SetCanvasImageInteractive(GameObject go, bool isInteractive)
        {
            var image = go.GetComponent<Image>();
            if (!image)
            {
                if (isInteractive)
                {
                    image = go.AddComponent<Image>();
                    image.color = Color.clear;
                }
                else
                {
                    return;
                }
            }
            image.raycastTarget = isInteractive;
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
            var sprites = AssetDatabase.LoadAllAssetsAtPath(mergeimg);
            foreach (var obj in sprites)
            {
                Sprite sprite = obj as Sprite;
                // if (sprite)
                // {
                //     len = sprites.Count;
                //     for (int i = 0; i < len; ++i)
                //     {
                //         if (sprites[i].name.Equals(objSprite.name))
                //         {
                //             sprites[i].sprite = objSprite;
                //         }
                //     }
                //     len = images.Count;
                //     for (int i = 0; i < len; ++i)
                //     {
                //         if (images[i].name.Equals(objSprite.name))
                //         {
                //             images[i].sprite = objSprite;
                //         }
                //     }
                // }
            }
        }

        private Dictionary<string, Dictionary<string, ConvertedSprite>> Plists = new Dictionary<string, Dictionary<string, ConvertedSprite>>();
        private Dictionary<string, ConvertedSprite> Sprites = new Dictionary<string, ConvertedSprite>();

        private Dictionary<string, ConvertedSprite> ImportPlist(string plist)
        {
            if (plist == null)
            {
                return null;
            }
            Dictionary<string, ConvertedSprite> spriteMap;
            if (!Plists.TryGetValue(plist, out spriteMap))
            {
                spriteMap = new Dictionary<string, ConvertedSprite>();
                Plists.Add(plist, spriteMap);
                var plistPath = TryGetFullResPath(plist);
                var imgPath = TryGetFullResPath(Path.ChangeExtension(plist, ".png"));
                if (plistPath != null && imgPath != null)
                {
                    var assetPath = TryGetFullOutPath(imgPath);
                    File.Copy(imgPath, assetPath, true);
                    assetPath = TryGetPathFromAsset(assetPath);
                    AssetDatabase.ImportAsset(assetPath);

                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;

                    var pd = new PlistDocument();
                    pd.ReadFromFile(plistPath);
                    var size = StringToVector2(pd.root["metadata"].AsDict().values["size"].AsString());
                    var frames = pd.root["frames"].AsDict().values;
                    var spritesheet = new SpriteMetaData[frames.Count];
                    int idx = 0;
                    foreach (var frame in frames)
                    {
                        var convertedSprite = new ConvertedSprite();
                        convertedSprite.fullname = frame.Key;
                        convertedSprite.name = Path.GetFileNameWithoutExtension(convertedSprite.fullname);

                        var md = new SpriteMetaData();
                        md.name = convertedSprite.name;
                        Rect rect = default;
                        bool rotated = false;
                        var props = frame.Value.AsDict().values;
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
                        convertedSprite.rotated = rotated;
                        spriteMap.Add(convertedSprite.fullname, convertedSprite);
                    }
                    importer.spritesheet = spritesheet;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var obj in sprites)
                    {
                        Sprite sprite = obj as Sprite;
                        if (sprite)
                        {
                            // Debug.Log($"{sprite.name}  {sprite.packed}  {sprite.packingMode}  {sprite.packingRotation}");
                            foreach (var pairs in spriteMap)
                            {
                                var convertedSprite = pairs.Value;
                                if (convertedSprite.name == sprite.name)
                                {
                                    convertedSprite.sprite = sprite;
                                    break;
                                }
                            }
                        }
                    }
                }
                foreach (var pairs in spriteMap)
                {
                    var convertedSprite = pairs.Value;
                    if (!convertedSprite.sprite)
                    {
                        throw new Exception("bad sprite");
                    }
                }
            }
            return spriteMap;
        }

        private ConvertedSprite ImportSprite(string path)
        {
            if (path == null)
            {
                return null;
            }
            ConvertedSprite convertedSprite;
            if (!Sprites.TryGetValue(path, out convertedSprite))
            {
                convertedSprite = new ConvertedSprite();
                Sprites.Add(path, convertedSprite);
                convertedSprite.fullname = path;
                convertedSprite.name = Path.GetFileNameWithoutExtension(convertedSprite.fullname);
                convertedSprite.rotated = false;

                var imgPath = TryGetFullResPath(path);
                if (imgPath != null)
                {
                    var assetPath = TryGetFullOutPath(imgPath);
                    File.Copy(imgPath, assetPath, true);
                    assetPath = TryGetPathFromAsset(assetPath);
                    AssetDatabase.ImportAsset(assetPath);

                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;


                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var obj in sprites)
                    {
                        Sprite sprite = obj as Sprite;
                        if (sprite)
                        {
                            if (convertedSprite.name == sprite.name)
                            {
                                convertedSprite.sprite = sprite;
                                break;
                            }
                        }
                    }
                }
                if (!convertedSprite.sprite)
                {
                    throw new Exception("bad sprite");
                }
            }
            return convertedSprite;
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
