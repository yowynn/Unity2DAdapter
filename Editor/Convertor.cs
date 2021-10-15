using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Wynncs.Util;

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
        private Dictionary<string, int> ConvertedList = new Dictionary<string, int>();
        public string ProjectPath;
        public List<string> ResPath;
        public string InFolder;
        public string OutFolder;

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
            Debug.Log($"PROCESS CSDFILE {csdpath}");
            CsdParser parser = null;
            try
            {
                csdpath = csdpath.Replace('\\', '/');
                var fullpath = TryGetFullResPath(csdpath);
                var xml = XmlUtil.Open(fullpath);
                parser = new CsdParser(xml);


                // create GameObject
                Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
                var root = ConvertNode(parser.Node, ref map, null);
                root.name = parser.Name;

                // save controller and clips
                if (parser.Timelines != null)
                {
                    var mainClip = ConvertTimelines(parser.Timelines, root, ref map);
                    RemoveRedundantCurves(mainClip, root);      // 去除冗余轨道
                    RemoveRedundantKeyFrames(mainClip);         // 去除冗余关键帧
                    var clipPath = TryGetOutPath(csdpath, ".anim");
                    AssetDatabase.CreateAsset(mainClip, clipPath);
                    var controllerPath = TryGetOutPath(csdpath, ".controller");
                    var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, mainClip);
                    if (!root.TryGetComponent<Animator>(out var animator)) animator = root.AddComponent<Animator>();
                    AnimatorController.SetAnimatorController(animator, controller);
                    if (parser.Animations != null)
                    {
                        var clips = ConvertAnimationList(controller, parser.Animations, mainClip, parser.DefaultAnimation);
                        foreach (var pair in clips)
                        {
                            var path = TryGetOutPath(csdpath, ".anim");
                            var tarclip = pair.Value;
                            path = path.Substring(0, path.Length - 5) + "_" + pair.Key + ".anim";
                            AssetDatabase.CreateAsset(tarclip, path);
                        }
                    }
                    AssetDatabase.SaveAssets();
                }

                // save prefab
                var prefabPath = TryGetOutPath(csdpath, ".prefab");
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

                var s = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>();
                var canvas = (s != null && s.Length > 0) ? s[0] : null;
                if (canvas != null && canvas.gameObject.activeSelf)
                {
                    root.transform.SetParent(canvas.transform, false);
                }
                else
                {
                    GameObject.DestroyImmediate(root, false);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"PROCESS CSDFILE ERR: {e.Message}");
                if (parser != null)
                {
                    CsdType.LogNonAccessKey(csdpath + "#ERROR:" + e.Message, "Count");
                }
                return;
            }
            MarkConverted(csdpath);
            Debug.Log($"PROCESS CSDFILE END {csdpath}");
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

        private Dictionary<string, AnimationClip> ConvertAnimationList(AnimatorController ac, Dictionary<string, CsdAnimInfo> animations, AnimationClip clip, string defaultStateName = "")
        {
            var clips = new Dictionary<string, AnimationClip>();
            if (ac.layers.Length == 0) ac.AddLayer("Base Layer");
            var stateMachine = ac.layers[0].stateMachine;
            foreach (var pair in animations)
            {
                var Name = pair.Key;
                var state = stateMachine.AddState(Name);
                Debug.Log($"animation {Name}");
                var tarclip = CutAnimationClip(clip, pair.Value);
                SetAnimationClipLoop(tarclip, true);            // debug
                tarclip.wrapMode = WrapMode.Loop;
                clips.Add(Name, tarclip);
                state.motion = tarclip;
                if (defaultStateName == Name)
                {
                    stateMachine.defaultState = state;
                }
                ac.AddParameter(Name, AnimatorControllerParameterType.Trigger);
                var trans = stateMachine.AddAnyStateTransition(state);
                trans.hasExitTime = true;
                trans.AddCondition(AnimatorConditionMode.If, 1f, Name);
            }
            return clips;
        }

        private AnimationClip CutAnimationClip(AnimationClip clip, CsdAnimInfo info)
        {
            var newclip = new AnimationClip();
            var startTime = info.StartTime;
            var endTime = info.EndTime;

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var newkeys = new List<Keyframe>();
                bool startFrame = true, endFrame = true;
                foreach (var key in curve.keys)
                {
                    if (key.time >= startTime && key.time <= endTime)
                    {
                        newkeys.Add(key);
                        if (key.time == startTime) startFrame = false;
                        if (key.time == endTime) endFrame = false;
                    }
                }
                if (startFrame)
                {
                    var val = curve.Evaluate(startTime);
                    newkeys.Add(new Keyframe(startTime, val));
                }
                if (endFrame)
                {
                    var val = curve.Evaluate(endTime);
                    newkeys.Add(new Keyframe(endTime, val));
                }
                var newcurve = new AnimationCurve(newkeys.ToArray());
                AnimationUtility.SetEditorCurve(newclip, binding, newcurve);
            }

            var obindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in obindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                var newkeys = new List<ObjectReferenceKeyframe>();
                bool startFrame = true, endFrame = true;
                ObjectReferenceKeyframe startKey = default, endKey = default;
                foreach (var key in keys)
                {
                    if (key.time >= startTime && key.time <= endTime)
                    {
                        newkeys.Add(key);
                        if (key.time == startTime) startFrame = false;
                        if (key.time == endTime) endFrame = false;
                    }
                    if (key.time < startTime && key.time >= startKey.time)
                    {
                        startKey = key;
                    }
                    if (key.time < endTime && key.time >= endKey.time)
                    {
                        endKey = key;
                    }
                }
                if (startFrame)
                {
                    var newkey = new ObjectReferenceKeyframe();
                    newkey.time = startTime;
                    newkey.value = startKey.value;
                    newkeys.Add(newkey);
                }
                if (endFrame)
                {
                    var newkey = new ObjectReferenceKeyframe();
                    newkey.time = endTime;
                    newkey.value = endKey.value;
                    newkeys.Add(newkey);
                }
                AnimationUtility.SetObjectReferenceCurve(newclip, binding, newkeys.ToArray());
            }
            return newclip;
        }

        private void SetAnimationClipLoop(AnimationClip clip, bool isLoop)
        {
            clip.wrapMode = isLoop ? WrapMode.Loop : WrapMode.Once;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = isLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private string GetGameObjectPath(GameObject go, GameObject root)
        {
            return AnimationUtility.CalculateTransformPath(go.transform, root.transform);
        }

        private int RemoveRedundantCurves(AnimationClip clip, GameObject linked)
        {
            var allCount = 0;
            var redundantCount = 0;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                bool isRedundant = true;
                float constVal = keys.Length > 0 ? keys[0].value : default;
                foreach (var key in keys)
                {
                    if (key.value != constVal)
                    {
                        isRedundant = false;
                        break;
                    }
                }
                // if (isRedundant)
                // {
                //     float linkedValue = TODO_GetBindingValueFloat(linked, binding);
                //     if (linkedValue != constVal)
                //     {
                //         isRedundant = false;
                //     }
                // }
                if (isRedundant)
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    // Debug.Log($"RedundantCurve: {binding.path}  {binding.type} {binding.propertyName}");
                    redundantCount++;
                }
                allCount++;
            }

            var obindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in obindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                bool isRedundant = true;
                var constVal = keys.Length > 0 ? keys[0].value : default;
                foreach (var key in keys)
                {
                    if (key.value != constVal)
                    {
                        isRedundant = false;
                        break;
                    }
                }
                // if (isRedundant)
                // {
                //     var linkedValue = TODO_GetBindingValueObject(linked, binding);
                //     if (linkedValue != constVal)
                //     {
                //         isRedundant = false;
                //     }
                // }
                if (isRedundant)
                {
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                    // Debug.Log($"RedundantCurve: {binding.path}  {binding.type} {binding.propertyName}");
                    redundantCount++;
                }
                allCount++;
            }
            Debug.Log($"RedundantCurveCount: {redundantCount} / {allCount}");
            return redundantCount;
        }

        private int RemoveRedundantKeyFrames(AnimationClip clip)
        {
            var allCount = 0;
            var redundantCount = 0;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                var list = new List<Keyframe>(keys);
                for (int i = list.Count - 2; i > 0; i--)
                {
                    var key = list[i];
                    if (key.value == keys[i - 1].value && key.value == keys[i + 1].value)
                    {
                        list.RemoveAt(i);
                    }
                }
                var newkeys = list.ToArray();
                allCount += keys.Length;
                redundantCount += keys.Length - newkeys.Length;
                var newcurve = new AnimationCurve(newkeys);
                AnimationUtility.SetEditorCurve(clip, binding, newcurve);
            }

            var obindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in obindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                var list = new List<ObjectReferenceKeyframe>(keys);
                for (int i = list.Count - 2; i > 0; i--)
                {
                    var key = list[i];
                    if (key.value == keys[i - 1].value && key.value == keys[i + 1].value)
                    {
                        list.RemoveAt(i);
                    }
                }

                var newkeys = list.ToArray();
                allCount += keys.Length;
                redundantCount += keys.Length - newkeys.Length;
                AnimationUtility.SetObjectReferenceCurve(clip, binding, newkeys);
            }
            Debug.Log($"RedundantKeyFrameCount: {redundantCount} / {allCount}");
            return redundantCount;
        }

        public bool IsConverted(string respath)
        {
            respath = respath.Replace('\\', '/');
            return ConvertedList.TryGetValue(respath, out var _);
        }

        public int MarkConverted(string respath)
        {
            respath = respath.Replace('\\', '/');
            if(!ConvertedList.TryGetValue(respath, out var count))
            {
                count = 0;
            }
            ConvertedList[respath] = ++count;
            return count;
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
            if (respath.StartsWith(InFolder))
            {
                outpath = OutFolder + respath.Substring(InFolder.Length, respath.Length - InFolder.Length);
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
            MarkConverted(plist);
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
                    // throw new Exception("bad sprite");
                    Debug.Log($"ERROR: sprite not find: {path}");
                }
            }
            MarkConverted(path);
            return convertedSprite;
        }

        protected ConvertedSprite GetSprite(CsdFileLink imageData)
        {
            ConvertedSprite convertedSprite = null;
            var list = ImportPlist(imageData.Plist);
            if (list != null)
            {
                list.TryGetValue(imageData.Path, out convertedSprite);
            }
            if (convertedSprite == null)
            {
                convertedSprite = ImportSprite(imageData.Path);
            }
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
                else
                {
                    MarkConverted(csdFile);
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



        public class ProjectsConvertor<TarConvertor> where TarConvertor : Convertor, new()
        {
            public struct ProjectInfo
            {
                public string projectName;
                public string projectPath;
                public string srcResPath;
                public string expResPath;
                public string findPath;
            }
            public List<ProjectInfo> Projects;
            public string OutFolder;

            public string RelativeSrcResPath { get; set; } = "cocosstudio";
            public string RelativeExpResPath { get; set; } = "res";

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
                path = FileSystem.GetPathInfo(path)?.FullName;
                if (path == null)
                {
                    throw new Exception("path not find");
                }
                Projects = new List<ProjectInfo>();
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
                            projectInfo.findPath = path.Replace(projectInfo.projectPath + "\\", "");
                            Projects.Add(projectInfo);
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
                            var projectInfo = new ProjectInfo();
                            projectInfo.projectPath = Path.GetDirectoryName(f.FullName);
                            projectInfo.projectName = Path.GetFileName(projectInfo.projectPath);
                            projectInfo.srcResPath = projectInfo.projectPath + "\\" + RelativeSrcResPath + "\\";
                            projectInfo.expResPath = projectInfo.projectPath + "\\" + RelativeExpResPath + "\\";
                            projectInfo.findPath = RelativeSrcResPath;
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
                Debug.Log($"PROCESS PROJECT {project.projectName}");
                var findPath = project.projectPath + "\\" + project.findPath;
                TarConvertor convertor = new TarConvertor();
                convertor.SetRootPath(project.srcResPath, new string[] { project.expResPath, });
                convertor.SetMapPath(FileSystem.GetFolderPath(findPath), OutFolder + "\\" + project.projectName);
                FileSystem.EnumPath(findPath, f =>
                {
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".csd")
                    {
                        var csdresname = f.FullName.Replace(project.srcResPath, "");
                        if (!convertor.IsConverted(csdresname))
                        {
                            convertor.Convert(csdresname);
                        }
                        else
                        {
                            convertor.MarkConverted(csdresname);
                        }
                    }
                    if (!FileSystem.IsFolder(f) && f.Extension.ToLower() == ".plist")
                    {
                        var plistresname = f.FullName.Replace(project.expResPath, "");
                        if (!convertor.IsConverted(plistresname))
                        {
                            convertor.ImportPlist(plistresname);
                        }
                        else
                        {
                            convertor.MarkConverted(plistresname);
                        }
                    }
                });
                Debug.Log($"PROCESS PROJECT END {project.projectName}");
                Cocos2Unity.CsdType.SwapAccessLog(OutFolder + "\\" + project.projectName + "_unhandled.xml");
            }
        }

    }
}
