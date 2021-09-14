using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cocos2Unity
{
    public abstract class Convertor
    {
        public class ConvertedSprite
        {
            public string name;
            public string fullname;
            public Sprite sprite;
            public bool rotated;
        }

        public static System.Random Random = new System.Random();
        public static Wynnsharp.XmlUtil XML = new Wynnsharp.XmlUtil();
        public string ProjectPath;
        public List<string> ResPath;
        public string InFolder;
        public string OutFolder;
        private CsdParser parser;

        protected abstract GameObject CreateGameObject(CsdNode node, GameObject parent = null);
        protected abstract void BindAnimationCurve(AnimationClip clip, GameObject go, string relativePath, CsdTimeline timeline);
        public void SetRootPath(string projectPath, string[] resPath)
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

        public void SetMapPath(string inFolder, string outFolder)
        {
            if (Directory.Exists(inFolder))
            {
                inFolder = inFolder.Replace('\\', '/');
                inFolder += inFolder.EndsWith("/") ? "" : "/";
                inFolder = TryGetPathFromProject(inFolder);
                if (inFolder != null)
                {
                    InFolder = inFolder;
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
            outFolder += outFolder.EndsWith("/") ? "" : "/";
            outFolder = TryGetPathFromAsset(outFolder);
            if (outFolder != null)
            {
                OutFolder = outFolder;
            }
            else
            {
                throw new Exception();
            }
        }

        public void Convert(string csdpath)
        {
            csdpath = csdpath.Replace('\\', '/');
            var fullpath = TryGetFullResPath(csdpath);
            var xml = XML.OpenXml(fullpath);
            parser = new CsdParser(xml);

            Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
            var root = ConvertNode(parser.Node, ref map, null);
            var clip = ConvertTimelines(parser.Timelines, root, ref map);
            var controller = ConvertAnimationList(null, root, clip);
            // var anim = root.AddComponent<Animator>();
            // anim.runtimeAnimatorController = Resources.Load
            // anim.AddClip(clip, "hah");

            var s = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>()[0];
            root.transform.SetParent(s.transform, false);



            var controllerPath = TryGetOutPath(csdpath, ".controller");
            AssetDatabase.CreateAsset(controller, controllerPath);

            var clipPath = TryGetOutPath(csdpath, ".anim");
            AssetDatabase.CreateAsset(clip, clipPath);

            var prefabPath = TryGetOutPath(csdpath, ".prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }

        private GameObject ConvertNode(CsdNode node, ref Dictionary<string, GameObject> map, GameObject parent)
        {
            GameObject go = null;
            if (node != null)
            {
                var Children = node.Children;
                node.Children = null;
                go = CreateGameObject(node, parent);
                node.Children = Children;
                string ActionTag = node.ActionTag;
                if (ActionTag != null && ActionTag != "")
                    map.Add(ActionTag, go);
                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        ConvertNode(child, ref map, go);
                    }
                }
            }
            return go;
        }

        private AnimationClip ConvertTimelines(Dictionary<string, CsdTimeline> timelines, GameObject root, ref Dictionary<string, GameObject> map)
        {
            AnimationClip clip = null;
            if (timelines != null)
            {
                clip = new AnimationClip();
                foreach (var pair in timelines)
                {
                    string ActionTag = pair.Key;
                    CsdTimeline Timeline = pair.Value;
                    if (map.TryGetValue(ActionTag, out var go))
                    {
                        var path = GetGameObjectPath(go, root);
                        BindAnimationCurve(clip, go, path, Timeline);
                    }
                }
            }
            return clip;
        }

        private UnityEditor.Animations.AnimatorController ConvertAnimationList(object animationList, GameObject root, AnimationClip clip)
        {
            if (!root.TryGetComponent<Animator>(out var animator))
            {
                animator = root.AddComponent<Animator>();
            }
            var ac = new UnityEditor.Animations.AnimatorController();
            ac.AddLayer("Base Layer");
            var stateMachine = ac.layers[0].stateMachine;
            var state = stateMachine.AddState("Test");
            // UnityEngine.Animations.AnimationClipPlayable p = UnityEngine.Animations.AnimationClipPlayable.Create(clip);
            state.motion = clip;
            UnityEditor.Animations.AnimatorController.SetAnimatorController(animator, ac);
            return ac;
            // UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/Mecanim/StateMachineTransitions.controller");
            // UnityEditor.Animations.AnimatorController.c
        }

        private string GetGameObjectPath(GameObject go, GameObject root)
        {
            string path = "";
            if (go != root)
            {
                var parent = go.transform.parent.gameObject;
                if (parent == root)
                    path = go.name;
                else
                    path = GetGameObjectPath(parent, root) + "/" + go.name;
            }
            return path;
        }

        public bool IsConverted(string respath)
        {
            respath = respath.Replace('\\', '/');
            var prefabPath = TryGetOutPath(respath, ".prefab");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private string TryGetFullResPath(string respath)
        {
            foreach (var p in ResPath)
            {
                if (File.Exists(p + respath))
                {
                    return p + respath;
                }
            }
            return null;
        }

        private string TryGetOutPath(string respath, string changedExtension = null)
        {
            string outpath;
            if (respath.Contains(InFolder))
            {
                outpath = respath.Replace(InFolder, OutFolder);
            }
            else
            {
                outpath = OutFolder + Path.GetFileName(respath);
            }
            if (changedExtension != null)
            {
                outpath = Path.ChangeExtension(outpath, changedExtension);
            }
            return outpath;
        }

        private string TryGetFullOutPath(string outpath)
        {
            if (outpath.StartsWith("Assets"))
            {
                var assetPath = Application.dataPath.Replace('\\', '/');
                var fullpath = outpath.Replace("Assets", assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
                return fullpath;
            }
            return null;
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

        private string TryGetPathFromProject(string fullpath)
        {
            fullpath = fullpath.Replace('\\', '/');
            if (fullpath.Contains(ProjectPath))
            {
                return fullpath.Replace(ProjectPath, "");
            }
            return null;
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
                var imgPath = Path.ChangeExtension(plist, ".png");
                var imgFromPath = TryGetFullResPath(imgPath);
                if (plistPath != null && imgFromPath != null)
                {
                    var assetPath = TryGetOutPath(imgPath);
                    var imgToPath = TryGetFullOutPath(assetPath);
                    File.Copy(imgFromPath, imgToPath, true);
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

                var imgFromPath = TryGetFullResPath(path);
                if (imgFromPath != null)
                {
                    var assetPath = TryGetOutPath(path);
                    var imgToPath = TryGetFullOutPath(assetPath);
                    File.Copy(imgFromPath, assetPath, true);
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

        protected ConvertedSprite GetSprite(CsdFileLink imageData)
        {
            var convertedSprite = ImportPlist(imageData.Plist)?[imageData.Path] ?? ImportSprite(imageData.Path);
            return convertedSprite;
        }

        protected GameObject CreateFromPrefab(CsdFileLink prefabData)
        {
            GameObject go = null;
            if (prefabData == null)
            {
                go = new GameObject();
            }
            else
            {
                var csdFile = prefabData.Path;
                if (!IsConverted(csdFile))
                {
                    Convert(csdFile);
                }
                var prefabPath = TryGetOutPath(csdFile, ".prefab");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab)
                {
                    go = GameObject.Instantiate<GameObject>(prefab);
                }
                else
                {
                    go = new GameObject();
                }
            }
            return go;
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

        private static Func<Convertor> ConvertorGroupNew;



        public class ConvertorProjects<TarConvertor> where TarConvertor : Convertor, new()
        {
            public static Wynnsharp.FileSystemUtil FS = new Wynnsharp.FileSystemUtil();

            public struct ProjectInfo
            {
                public string projectName;
                public string projectPath;
                public string cocosstudioPath;
                public string resPath;
                public string findPath;
            }
            public List<ProjectInfo> Projects;
            public string OutFolder;

            public void Convert(string path, string outPath)
            {
                AnalyseInPath(path);
                AnalyseOutPath(outPath);
                foreach (var project in Projects)
                {
                    ConvertorProject(project);
                }
            }

            private void AnalyseInPath(string path)
            {
                path = FS.GetPathInfo(path)?.FullName;
                if (path == null)
                {
                    throw new Exception("path not find");
                }
                Projects = new List<ProjectInfo>();
                var found = false;
                var parentpath = Path.GetDirectoryName(path);
                while (parentpath != null && !found)
                {
                    FS.EnumPath(parentpath, f =>
                    {
                        if (!FS.IsFolder(f) && f.Extension.ToLower() == ".ccs" && !found)
                        {
                            var projectInfo = new ProjectInfo();
                            projectInfo.projectPath = Path.GetDirectoryName(f.FullName);
                            projectInfo.projectName = Path.GetFileName(projectInfo.projectPath);
                            projectInfo.cocosstudioPath = projectInfo.projectPath + "\\cocosstudio\\";
                            projectInfo.resPath = projectInfo.projectPath + "\\res\\";
                            projectInfo.findPath = path.Replace(projectInfo.projectPath + "\\", "");
                            Projects.Add(projectInfo);
                            found = true;
                        }
                    }, false);
                    parentpath = Path.GetDirectoryName(parentpath);
                }
                if (!found)
                {
                    FS.EnumPath(path, f =>
                    {
                        if (!FS.IsFolder(f) && f.Extension.ToLower() == ".ccs")
                        {
                            var projectInfo = new ProjectInfo();
                            projectInfo.projectPath = Path.GetDirectoryName(f.FullName);
                            projectInfo.projectName = Path.GetFileName(projectInfo.projectPath);
                            projectInfo.cocosstudioPath = projectInfo.projectPath + "\\cocosstudio\\";
                            projectInfo.resPath = projectInfo.projectPath + "\\res\\";
                            projectInfo.findPath = "cocosstudio";
                            Projects.Add(projectInfo);
                        }
                    });
                }
            }

            private void AnalyseOutPath(string path)
            {
                if (TryGetPathFromAsset(path) == null)
                {
                    throw new Exception("outpath must in Assets");
                }
                OutFolder = Directory.CreateDirectory(path).FullName;
            }

            private void ConvertorProject(ProjectInfo project)
            {
                var findPath = project.projectPath + "\\" + project.findPath;
                TarConvertor convertor = new TarConvertor();
                convertor.SetRootPath(project.cocosstudioPath, new string[] { project.resPath, });
                convertor.SetMapPath(FS.GetPath(findPath), OutFolder + "\\" + project.projectName);
                FS.EnumPath(findPath, f =>
                {
                    if (!FS.IsFolder(f) && f.Extension.ToLower() == ".csd")
                    {
                        var csdresname = f.FullName.Replace(project.cocosstudioPath, "");
                        if (!convertor.IsConverted(csdresname))
                        {
                            convertor.Convert(csdresname);
                        }
                    }
                });
                Cocos2Unity.CsdType.SwapAccessLog(convertor.OutFolder + project.projectName + "_unhandled.xml");
            }
        }

    }
}
