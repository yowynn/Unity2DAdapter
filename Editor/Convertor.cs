using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Wynncs.Util;
using System.Linq;

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

        private static System.Object BadObject = new System.Object();
        // private Dictionary<string, int> ConvertedList = new Dictionary<string, int>();
        public string ProjectPath;
        public List<string> FindPath;
        public string InFolder;
        public string OutFolder;
        public Dictionary<string, System.Object> ConvertedObjects = new Dictionary<string, System.Object>();

        protected abstract GameObject CreateNode(CsdNode node, GameObject parent = null);
        protected abstract void BindAnimationCurves(AnimationClip clip, GameObject go, string relativePath, CsdTimeline timeline);

        public void SetRootPath(string projectPath, string[] resPath)
        {
            if (Directory.Exists(projectPath))
            {
                projectPath = projectPath.Replace('\\', '/');
                projectPath += projectPath.EndsWith("/") ? "" : "/";
                ProjectPath = projectPath;

                FindPath = new List<string>();
                FindPath.Add(projectPath);
                if (resPath != null)
                {
                    foreach (string path in resPath)
                    {
                        if (Directory.Exists(path))
                        {
                            string p = path.Replace('\\', '/');
                            p += p.EndsWith("/") ? "" : "/";
                            if (!FindPath.Contains(p))
                            {
                                FindPath.Add(p);
                            }
                        }
                    }
                }
            }
            else
            {
                ProjectPath = null;
                FindPath = null;
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

        public GameObject ConvertCsd(string csdpath, bool forceReconvert)
        {
            if (csdpath == null)
            {
                return null;
            }
            csdpath = csdpath.Replace('\\', '/');
            if (ConvertedObjects.TryGetValue(csdpath, out var o) && !forceReconvert)
            {
                return o as GameObject;
            }
            else
            {
                var parser = new CsdParser(XmlUtil.Open(TryGetFullResPath(csdpath)));
                GameObject target = ConvertCsdViaParser(csdpath);

                ConvertedObjects.Add(csdpath, target ?? BadObject);
                return target;
            }
        }

        public Dictionary<string, ConvertedSprite> ConvertCsi(string csipath, bool forceReconvert)
        {
            if (csipath == null)
            {
                return null;
            }
            csipath = csipath.Replace('\\', '/');
            if (ConvertedObjects.TryGetValue(csipath, out var o) && !forceReconvert)
            {
                return o as Dictionary<string, ConvertedSprite>;
            }
            else
            {
                var plistpath = Path.ChangeExtension(csipath, ".plist");
                Dictionary<string, ConvertedSprite> target = ConvertPlistViaParser(plistpath);
                ConvertedObjects.Add(csipath, target ?? BadObject);
                return target;
            }
        }

        protected GameObject GetSubnode(CsdFileLink prefabData)
        {
            GameObject go = null;
            if (prefabData == null)
            {
                go = new GameObject();
            }
            else
            {
                var csd = prefabData.Path;
                Debug.Log($"PROCESS SUB : {csd}");
                GameObject prefab = ConvertCsd(csd, false);
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

        protected ConvertedSprite GetSprite(CsdFileLink imageData)
        {
            ConvertedSprite convertedSprite = null;
            var plist = imageData.Plist;
            var pngpath = imageData.Path;
            if (plist != null && plist != "")
            {
                var csi = Path.ChangeExtension(plist, ".csi");
                var list = ConvertCsi(csi, false);
                if (list != null)
                {
                    list.TryGetValue(pngpath, out convertedSprite);
                }
            }
            if (convertedSprite == null)
            {
                if (ConvertedObjects.TryGetValue(pngpath, out var o))
                {
                    convertedSprite = o as ConvertedSprite;
                }
                else
                {
                    var fromFullpath = TryGetFullResPath(pngpath);
                    if (fromFullpath != null)
                    {
                        var assetPath = TryGetOutPath(pngpath);
                        var toFullpath = TryGetFullOutPath(assetPath);
                        File.Copy(fromFullpath, toFullpath, true);
                        convertedSprite = ImportSprite(assetPath, pngpath);
                    }
                    ConvertedObjects.Add(pngpath, convertedSprite ?? BadObject);
                }

            }
            return convertedSprite;
        }

        private GameObject ConvertNode(CsdNode node, ref Dictionary<string, GameObject> map, GameObject parent)
        {
            GameObject go = null;
            if (node != null)
            {
                var Children = node.Children;
                node.Children = null;
                go = CreateNode(node, parent);
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

        private AnimationClip ConvertTimelines(Dictionary<string, CsdTimeline> timelines, GameObject root, ref Dictionary<string, GameObject> map, int frameRate = 60)
        {
            AnimationClip clip = null;
            if (timelines != null)
            {
                clip = new AnimationClip();
                clip.frameRate = frameRate;
                foreach (var pair in timelines)
                {
                    string ActionTag = pair.Key;
                    CsdTimeline Timeline = pair.Value;
                    if (map.TryGetValue(ActionTag, out var go))
                    {
                        var path = GetGameObjectPath(go, root);
                        BindAnimationCurves(clip, go, path, Timeline);
                    }
                }
                clip.EnsureQuaternionContinuity();
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
            newclip.frameRate = clip.frameRate;
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
            var markedRedundant = new Dictionary<EditorCurveBinding, bool>();
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
                //     AnimationUtility.SetEditorCurve(clip, binding, null);
                //     // Debug.Log($"RedundantCurve: {binding.path}  {binding.type} {binding.propertyName}");
                //     redundantCount++;
                // }
                markedRedundant[binding] = isRedundant;
                allCount++;
            }
            Func<string, string> getPre = p =>
            {
                var i = p.LastIndexOf('.');
                if (i == -1)
                    return p;
                else
                    return p.Substring(0, i + 1);
            };
            Func<IEnumerable<EditorCurveBinding>, int> GetUsed = list =>
            {
                int count = 0;
                foreach (var item in list)
                {
                    if (markedRedundant[item] == false)
                    {
                        ++count;
                    }
                }
                return count;
            };

            var Redundants =
                from binding in bindings
                group binding by new { binding.path, binding.type, propertyPrev = getPre(binding.propertyName) } into Group
                where GetUsed(Group) == 0
                select Group;
            foreach(var group in Redundants)
            {
                foreach(var binding in group)
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    redundantCount++;
                }
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

        private GameObject ConvertCsdViaParser(string csdpath)
        {
            try
            {
                var fullpath = TryGetFullResPath(csdpath);
                if (fullpath == null)
                {
                    throw new Exception("file not found");
                }

                CsdParser parser = new CsdParser(XmlUtil.Open(fullpath));

                // create GameObject
                Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
                var root = ConvertNode(parser.Node, ref map, null);
                root.name = parser.Name;

                // save controller and clips
                if (parser.Timelines != null)
                {
                    var mainClip = ConvertTimelines(parser.Timelines, root, ref map, parser.FrameRate);
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
                var TARGET = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

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
                return TARGET;
            }
            catch (Exception e)
            {
                CsdType.LogNonAccessKey(csdpath + "#ERROR:" + e.Message, "Count");
                return null;
            }
        }

        private Dictionary<string, ConvertedSprite> ConvertPlistViaParser(string plistpath)
        {
            try
            {
                var fullpath = TryGetFullResPath(plistpath);
                var fromPath = Path.ChangeExtension(plistpath, ".png");
                var fromFullpath = TryGetFullResPath(fromPath);
                if (fullpath == null || fromFullpath == null)
                {
                    throw new Exception("file not found");
                }

                var parser = new PlistDocument();
                parser.ReadFromFile(fullpath);

                var toPath = TryGetOutPath(fromPath);
                var toFullpath = TryGetFullOutPath(toPath);
                File.Copy(fromFullpath, toFullpath, true);
                var TARGET = ImportSpriteFromPlist(toPath, parser);
                return TARGET;
            }
            catch (Exception e)
            {
                CsdType.LogNonAccessKey(plistpath + "#ERROR:" + e.Message, "Count");
                return null;
            }
        }

        private Dictionary<string, ConvertedSprite> ImportSpriteFromPlist(string assetPath, PlistDocument plist)
        {
            var spriteList = new Dictionary<string, ConvertedSprite>();
            AssetDatabase.ImportAsset(assetPath);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            var size = StringToVector2(plist.root["metadata"].AsDict().values["size"].AsString());
            var frames = plist.root["frames"].AsDict().values;
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
                                // var val = StringToVector2(prop.Value.AsString());
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
                                // var val = StringToVector2(prop.Value.AsString());
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
                spriteList.Add(convertedSprite.fullname, convertedSprite);
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
                    foreach (var pairs in spriteList)
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
            foreach (var pairs in spriteList)
            {
                var convertedSprite = pairs.Value;
                if (!convertedSprite.sprite)
                {
                    throw new Exception("bad sprite");
                }
            }
            return spriteList;
        }

        private ConvertedSprite ImportSprite(string assetPath, string resName)
        {
            var convertedSprite = new ConvertedSprite();
            convertedSprite.fullname = resName;
            convertedSprite.name = Path.GetFileNameWithoutExtension(convertedSprite.fullname);
            convertedSprite.rotated = false;

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
                        return convertedSprite;
                    }
                }
            }
            return null;
        }

        public bool IsConverted(string respath)
        {
            return ConvertedObjects.TryGetValue(respath, out var _);
        }

        private string TryGetFullResPath(string respath)
        {
            foreach (var p in FindPath)
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
