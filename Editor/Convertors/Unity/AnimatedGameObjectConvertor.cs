using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cocos2Unity.Models;
using Cocos2Unity.Util;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Animations;

namespace Cocos2Unity.Unity
{
    public abstract class AnimatedGameObjectConvertor : IConvertor
    {
        # region Implemented IParser
        public void ConvertNodePackage(string assetpath, Func<string, NodePackage> GetNodePackage)
        {
            ProcessLog.Log($"--Import NODE Assert: {assetpath}");
            CreateAssetFolder(assetpath);
            NodePackage nodePackage = GetNodePackage(assetpath);
            if (nodePackage == null)
            {
                ProcessLog.LogError("Can't find the NodePackage" + assetpath);
                return;
            }
            if (convertedNodePackages.ContainsKey(assetpath))
            {
                return;
            }
            foreach (var linkedNode in nodePackage.LinkedNodes)
            {
                ConvertNodePackage(linkedNode.Name, GetNodePackage);
            }
            GameObject gameObject = CreateAndSaveGameObject(assetpath, nodePackage);
            convertedNodePackages.Add(assetpath, gameObject);
        }

        public void ConvertSpriteList(string assetpath, Func<string, SpriteList> GetSpriteList)
        {
            ProcessLog.Log($"--Import ATLAS Assert: {assetpath}");
            CreateAssetFolder(assetpath);
            SpriteList spriteList = GetSpriteList(assetpath);
            if (spriteList == null)
            {
                ProcessLog.LogError("Can't find the SpriteList: " + assetpath);
                return;
            }
            if (convertedSpriteLists.ContainsKey(assetpath))
            {
                return;
            }
            SpriteAtlas spriteAtlas = CreateAndSaveSpriteAtlas(assetpath, spriteList);
            convertedSpriteLists.Add(assetpath, spriteAtlas);
        }

        public void ImportUnparsedAsset(string assetpath, Func<string, string> GetFullPath)
        {
            ProcessLog.Log($"--Import OTHER Asset: {assetpath}");
            CreateAssetFolder(assetpath);
            string fullpath = GetFullPath(assetpath);
            if (fullpath == null)
            {
                ProcessLog.LogError("Can't find Asset: " + assetpath);
                return;
            }
            if (importedUnparsedAssetAssets.ContainsKey(assetpath))
            {
                return;
            }
            var extention = Path.GetExtension(assetpath).ToLower();
            switch (extention)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                    var sprite = ImportSprite(assetpath, fullpath);
                    importedUnparsedAssetAssets.Add(assetpath, sprite);
                    break;
                default:
                    ProcessLog.LogError("Unsupported asset type: " + assetpath);
                    break;
            }
        }

        public void SetOutputPath(string path)
        {
            path = Path.GetFullPath(path).Replace("\\", "/"); ;
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            var assetRoot = Application.dataPath.Replace('\\', '/');
            if (path.StartsWith(assetRoot))
            {
                FullOutputPath = path;
                OutputPath = path.Replace(assetRoot, "Assets");
            }
            else
            {
                throw new Exception("Output path must be in Assets folder");
            }
            importedUnparsedAssetAssets = new Dictionary<string, UnityEngine.Object>();
            convertedSpriteLists = new Dictionary<string, SpriteAtlas>();
            convertedNodePackages = new Dictionary<string, GameObject>();
        }

        # endregion

        public string OutputPath { get; private set; }
        public string FullOutputPath { get; private set; }
        private Dictionary<string, UnityEngine.Object> importedUnparsedAssetAssets;
        private Dictionary<string, SpriteAtlas> convertedSpriteLists;
        private Dictionary<string, GameObject> convertedNodePackages;

        protected abstract void BindComponentData(GameObject node, ModNode nodeData);
        protected abstract void BindAnimationCurves(GameObject root, AnimationClip clip, GameObject node, ModTimeline<ModNode> timeline);
        protected Sprite GetSprite(string assetpath)
        {
            importedUnparsedAssetAssets.TryGetValue(assetpath, out var sprite);
            ProcessLog.Log($"--Get Sprite: {assetpath}, {sprite as Sprite}");
            return sprite as Sprite;
        }

        protected GameObject GetGameObject(string assetpath = null)
        {
            if (assetpath == null)
            {
                return new GameObject();
            }
            convertedNodePackages.TryGetValue(assetpath, out var gameObject);
            return GameObject.Instantiate(gameObject);
        }

        protected void CreateAssetFolder(string assetpath)
        {
            var path = Path.GetDirectoryName(Path.Combine(FullOutputPath, assetpath));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private GameObject CreateAndSaveGameObject(string fromAssetPath, NodePackage nodePackage)
        {
            var toAssetPath = Path.ChangeExtension(Path.Combine(OutputPath, fromAssetPath), ".prefab");
            GameObject rootNode = ConvertFromNode(nodePackage.RootNode);
            rootNode.name = nodePackage.Name;
            BindAndSaveGameObjectAnimations(toAssetPath, rootNode, nodePackage.Animations, nodePackage.DefaultAnimationName);
            ProcessLog.Log($"-- * {toAssetPath}");
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootNode, toAssetPath, InteractionMode.AutomatedAction);
            GameObject.DestroyImmediate(rootNode);
            Debug_AddPrefabToSceneCanvas(prefab, toAssetPath);
            return prefab;
        }

        private GameObject ConvertFromNode(ModNode node, GameObject parent = null)
        {
            if (node == null)
            {
                return null;
            }
            GameObject gameObject = GetGameObject(node.Filler?.Node?.Name);
            gameObject.transform.SetParent(parent == null ? null : parent.transform, false);
            BindComponentData(gameObject, node);
            foreach (var child in node.Children)
            {
                ConvertFromNode(child, gameObject);
            }
            return gameObject;
        }

        private void BindAndSaveGameObjectAnimations(string rootNodeAssetPath, GameObject rootNode, IDictionary<string, ModNodeAnimation> animations, string defaultAnimationName = null)
        {
            var controllerAssetPath = Path.ChangeExtension(rootNodeAssetPath, ".controller");
            ProcessLog.Log($"-- * {controllerAssetPath}");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerAssetPath);
            if (!rootNode.TryGetComponent<Animator>(out var animator))
            {
                animator = rootNode.AddComponent<Animator>();
            }
            AnimatorController.SetAnimatorController(animator, controller);
            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }
            var stateMachine = controller.layers[0].stateMachine;

            var convertedTimelines = new Dictionary<ModNodeAnimationAtlas, AnimationClip>();
            foreach (var pair in animations)
            {
                var name = pair.Key;
                var animation = pair.Value;
                if (!convertedTimelines.TryGetValue(animation.AnimationAtlas, out AnimationClip baseClip))
                {
                    baseClip = CreateBindingAnimationClip(rootNode, animation.AnimationAtlas);
                    convertedTimelines.Add(animation.AnimationAtlas, baseClip);
                }
                var state = stateMachine.AddState(name);
                AnimationClip clip = CutAnimationClip(baseClip, animation.TimeFrom, animation.TimeTo);
                Debug_SetAnimationClipLoop(clip, true);
                state.motion = clip;
                if (!string.IsNullOrEmpty(defaultAnimationName) && name == defaultAnimationName)
                {
                    stateMachine.defaultState = state;
                }
                controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
                var transition = stateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.If, 1f, name);
                transition.hasExitTime = true;

                var animAssetPath = Path.ChangeExtension(rootNodeAssetPath, $"{name}.anim");
                ProcessLog.Log($"-- * {animAssetPath}");
                AssetDatabase.CreateAsset(clip, animAssetPath);
            }
            AssetDatabase.SaveAssets();                         // TODO: Check if this is needed
        }

        private AnimationClip CreateBindingAnimationClip(GameObject rootNode, ModNodeAnimationAtlas atlas)
        {
            var clip = new AnimationClip();
            clip.frameRate = atlas.FrameRate;
            foreach (ModNode node in atlas.GetAnimatedNodes())
            {
                var timeline = atlas.GetTimeline(node);
                GameObject subNode = SearchNodeFromRoot(rootNode, node);
                BindAnimationCurves(rootNode, clip, subNode, timeline);
            }
            clip.EnsureQuaternionContinuity();
            Debug_OptimizeAnimationClip(clip, rootNode);        // 优化动画
            return clip;
        }

        private AnimationClip CutAnimationClip(AnimationClip baseClip, float timeFrom, float timeTo)
        {
            var clip = new AnimationClip();
            clip.frameRate = baseClip.frameRate;
            clip.legacy = baseClip.legacy;

            var bindings = AnimationUtility.GetCurveBindings(baseClip);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(baseClip, binding);
                var newkeys = new List<Keyframe>();
                bool includeFromKey = false, includeToKey = false;
                foreach (var key in curve.keys)
                {
                    if (key.time >= timeFrom && key.time <= timeTo)
                    {
                        newkeys.Add(key);
                        includeFromKey |= key.time == timeFrom;
                        includeToKey |= key.time == timeTo;
                    }
                }
                if (!includeFromKey) newkeys.Insert(0, new Keyframe(timeFrom, curve.Evaluate(timeFrom)));
                if (!includeToKey) newkeys.Add(new Keyframe(timeTo, curve.Evaluate(timeTo)));
                var newCurve = new AnimationCurve(newkeys.ToArray());
                AnimationUtility.SetEditorCurve(clip, binding, newCurve);
            }
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(baseClip);
            foreach (var objectBinding in objectBindings)
            {
                var curve = AnimationUtility.GetObjectReferenceCurve(baseClip, objectBinding);
                var newkeys = new List<ObjectReferenceKeyframe>();
                ObjectReferenceKeyframe frameFrom = default, frameTo = default;
                foreach (var key in curve)
                {
                    if (key.time > timeFrom && key.time < timeTo)
                    {
                        newkeys.Add(key);
                    }
                    if (key.time <= timeFrom && key.time >= frameFrom.time) frameFrom = key;
                    if (key.time <= timeTo && key.time >= frameTo.time) frameTo = key;
                }

                newkeys.Insert(0, frameFrom);
                if (frameTo.time > frameFrom.time) newkeys.Add(frameTo);
                var newCurve = newkeys.ToArray();
                AnimationUtility.SetObjectReferenceCurve(clip, objectBinding, newCurve);
            }
            return clip;
        }

        private static GameObject SearchNodeFromRoot(GameObject rootNode, ModNode node)
        {
            string path = node.Path.Substring(node.Path.IndexOf('/') + 1);
            var subNode = rootNode.transform.Find(path);
            if (subNode == null)
            {
                throw new Exception($"Node {node.Name} not found in {rootNode.name}");
            }
            return subNode.gameObject;
        }

        private void Debug_SetAnimationClipLoop(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private void Debug_OptimizeAnimationClip(AnimationClip clip, GameObject rootNode)
        {
            RemoveRedundantCurves(clip, rootNode);              // 去除冗余轨道
            RemoveRedundantKeyFrames(clip);                     // 去除冗余关键帧
        }

        private void RemoveRedundantCurves(AnimationClip clip, GameObject rootNode)
        {
            var markedUsefulCurves = new Dictionary<EditorCurveBinding, bool>();
            Func<string, string> MasterPropertyName = path => path.Split('.')[0];
            Func<IEnumerable<EditorCurveBinding>, bool> Redundant = bindingGroup => bindingGroup.Count(binding => markedUsefulCurves[binding]) == 0;

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                bool useful = false;
                float constantValue = keys.Length > 0 ? keys[0].value : default;
                foreach (var key in keys)
                {
                    if (key.value != constantValue)
                    {
                        useful = true;
                        break;
                    }
                }
                markedUsefulCurves[binding] = useful;
            }
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var objectBinding in objectBindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, objectBinding);
                bool useful = false;
                UnityEngine.Object constantValue = keys.Length > 0 ? keys[0].value : default;
                foreach (var key in keys)
                {
                    if (key.value != constantValue)
                    {
                        useful = true;
                        break;
                    }
                }
                markedUsefulCurves[objectBinding] = useful;
            }

            var redundantGroups = markedUsefulCurves.Keys.GroupBy(binding => new { binding.path, binding.type, masterPropertyName = MasterPropertyName(binding.propertyName) }).Where(group => Redundant(group));
            foreach (var group in redundantGroups)
            {
                foreach (var binding in group)
                {
                    if (bindings.Contains(binding))
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                    else
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                }
            }
            int beforeCount = markedUsefulCurves.Count;
            int afterCount = AnimationUtility.GetCurveBindings(clip).Length + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            ProcessLog.Log($"Remove redundant curves [{clip.name}], rest: {afterCount} / {beforeCount}");
        }

        private void RemoveRedundantKeyFrames(AnimationClip clip)
        {
            int beforeCount = 0;
            int afterCount = 0;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                var newkeys = new List<Keyframe>(keys);
                for (int i = newkeys.Count - 2; i > 0; --i)
                {
                    if (newkeys[i].value == newkeys[i + 1].value &&newkeys[i].value == newkeys[i - 1].value)
                    {
                        newkeys.RemoveAt(i);
                    }
                }
                beforeCount += keys.Length;
                afterCount += newkeys.Count;
                var newcurve = new AnimationCurve(newkeys.ToArray());
                AnimationUtility.SetEditorCurve(clip, binding, newcurve);
            }
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var objectBinding in objectBindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, objectBinding);
                var newkeys = new List<ObjectReferenceKeyframe>(keys);
                for (int i = newkeys.Count - 2; i > 0; --i)
                {
                    if (newkeys[i].value == newkeys[i + 1].value && newkeys[i].value == newkeys[i - 1].value)
                    {
                        newkeys.RemoveAt(i);
                    }
                }
                beforeCount += keys.Length;
                afterCount += newkeys.Count;
                var newcurve = newkeys.ToArray();
                AnimationUtility.SetObjectReferenceCurve(clip, objectBinding, newcurve);
            }
            ProcessLog.Log($"Remove redundant keyframes [{clip.name}], rest: {afterCount} / {beforeCount}");
        }

        private void Debug_AddPrefabToSceneCanvas(GameObject prefab, string assetpath)
        {
            if (prefab == null)
            {
                return;
            }
            var canvasList = GameObject.FindObjectsOfType<Canvas>();
            var canvas = canvasList.Length > 0 ? canvasList[0] : null;
            if (canvas != null && canvas.gameObject.activeSelf)
            {
                GameObject gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                gameObject.transform.SetParent(canvas.transform, false);
            }
        }

        private SpriteAtlas CreateAndSaveSpriteAtlas(string fromAssetPath, SpriteList spriteList)
        {
            var toAssetPath = Path.ChangeExtension(Path.Combine(OutputPath, fromAssetPath), ".spriteatlas");
            var atlas = new SpriteAtlas();
            atlas.name = spriteList.Name;
            List<Sprite> sprites = new List<Sprite>();
            foreach (var linkedSprite in spriteList.LinkedSprites)
            {
                var sprite = GetSprite(linkedSprite.Name);
                if (sprite == null)
                {
                    ProcessLog.LogError("Sprite not found: " + linkedSprite.Name);
                    continue;
                }
                sprites.Add(sprite);
            }
            atlas.Add(sprites.ToArray());

            SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = false && spriteList.AllowRotation;     // force to disable rotation in UI Canvas
            packingSettings.padding = spriteList.SpritePadding;
            // ! 因为Unity的SpriteAtlas的Packing方式是按照图片的最大边进行排列，而不是按照图片的最小边进行排列，所以这里需要设置一下
            packingSettings.enableTightPacking = false;
            atlas.SetPackingSettings(packingSettings);

            TextureImporterPlatformSettings defaultPlatformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            defaultPlatformSettings.maxTextureSize = (int)spriteList.MaxTextureSize.X;
            atlas.SetPlatformSettings(defaultPlatformSettings);
            ProcessLog.Log($"-- * {toAssetPath}");
            AssetDatabase.CreateAsset(atlas, toAssetPath);
            return atlas;
        }

        private Sprite ImportSprite(string fromAssetPath, string fromFullPath)
        {
            var toAssetPath = Path.Combine(OutputPath, fromAssetPath);
            var toFullPath = Path.Combine(FullOutputPath, fromAssetPath);
            ProcessLog.Log($"-- * {toAssetPath}");
            File.Copy(fromFullPath, toFullPath, true);

            AssetDatabase.ImportAsset(toAssetPath);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(toAssetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(toAssetPath);

            sprite.name = Path.GetFileNameWithoutExtension(fromAssetPath);
            return sprite;
        }


    }
}
