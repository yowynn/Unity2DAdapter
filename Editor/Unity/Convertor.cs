using System;
using Cocos2Unity.Models;
using System.IO;
using System.Collections.Generic;
using Wynncs.Util;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using UnityEditor.U2D;

namespace Cocos2Unity.Unity
{
    public class Convertor : IConvertor
    {
        # region Implemented IParser
        public void ConvertNodePackage(string assetpath, Func<string, NodePackage> GetNodePackage)
        {
            NodePackage nodePackage = GetNodePackage(assetpath);
            if (nodePackage == null)
            {
                Debug.LogError("Can't find the NodePackage" + assetpath);
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
            SpriteList spriteList = GetSpriteList(assetpath);
            if (spriteList == null)
            {
                Debug.LogError("Can't find the SpriteList: " + assetpath);
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
            string fullpath = GetFullPath(assetpath);
            if (fullpath == null)
            {
                Debug.LogError("Can't find Asset: " + assetpath);
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
                    Debug.LogError("Unsupported asset type: " + assetpath);
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

        protected Sprite GetSprite(string assetpath)
        {
            importedUnparsedAssetAssets.TryGetValue(assetpath, out var sprite);
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

        private GameObject CreateAndSaveGameObject(string fromAssetPath, NodePackage nodePackage)
        {
            var toAssetPath = Path.ChangeExtension(Path.Combine(OutputPath, fromAssetPath), ".prefab");
            GameObject rootNode = ConvertFromNode(nodePackage.RootNode);
            rootNode.name = nodePackage.Name;
            BindAndSaveGameObjectAnimations(toAssetPath, rootNode, nodePackage.Animations, nodePackage.DefaultAnimationName);
            // rootNode = PrefabUtility.SaveAsPrefabAssetAndConnect(rootNode, toAssetPath, InteractionMode.AutomatedAction);
            rootNode = PrefabUtility.SaveAsPrefabAsset(rootNode, toAssetPath);
            Debug_AddPrefabToSceneCanvas(rootNode);
            return rootNode;
        }

        private GameObject ConvertFromNode(ModNode node, GameObject parent = null)
        {
            if (node == null)
            {
                return null;
            }
            GameObject gameObject = GetGameObject(node.Filler?.Node?.Name);
            gameObject.transform.SetParent(parent == null ? null : parent.transform);
            SetComponentData(gameObject, node);
            foreach (var child in node.Children)
            {
                ConvertFromNode(child, gameObject);
            }
            return gameObject;
        }

        private void BindAndSaveGameObjectAnimations(string rootNodeAssetPath, GameObject rootNode, IDictionary<string, ModNodeAnimation> animations, string defaultAnimationName = null)
        {
            var convertedTimelines = new Dictionary<Dictionary<ModNode, ModTimeline<ModNode>>, AnimationClip>();
            foreach (var pair in animations)
            {
                var name = pair.Key;
                var animation = pair.Value;
                if (!convertedTimelines.TryGetValue(animation.Timelines, out var clip))
                {
                    clip = CreateBindingAnimationClip(rootNode, animation.Timelines);
                    convertedTimelines.Add(animation.Timelines, clip);
                }
            }
        }

        private void Debug_AddPrefabToSceneCanvas(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }
            var canvasList = GameObject.FindObjectsOfType<Canvas>();
            var canvas = canvasList.Length > 0 ? canvasList[0] : null;
            if (canvas != null && canvas.gameObject.activeSelf)
            {
                var gameObject = GameObject.Instantiate(prefab);
                gameObject.transform.SetParent(canvas.transform);
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
                if (sprite != null)
                {
                    Debug.LogError("Sprite not found: " + linkedSprite.Name);
                    continue;
                }
                sprites.Add(sprite);
            }
            atlas.Add(sprites.ToArray());

            SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = false && spriteList.AllowRotation;     // force to disable rotation in UI Canvas
            packingSettings.padding = spriteList.SpritePadding;
            atlas.SetPackingSettings(packingSettings);

            TextureImporterPlatformSettings defaultPlatformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            defaultPlatformSettings.maxTextureSize = (int)spriteList.MaxTextureSize.X;
            atlas.SetPlatformSettings(defaultPlatformSettings);

            AssetDatabase.CreateAsset(atlas, toAssetPath);
            return atlas;
        }

        private Sprite ImportSprite(string fromAssetPath, string fromFullPath)
        {
            var toAssetPath = Path.Combine(OutputPath, fromAssetPath);
            var toFullPath = Path.Combine(FullOutputPath, fromAssetPath);
            File.Copy(fromFullPath, toFullPath, true);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(toAssetPath);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = Path.GetFileNameWithoutExtension(fromAssetPath);
            // var dir = Path.GetDirectoryName(toAssetPath);
            // var name = Path.GetFileNameWithoutExtension(toAssetPath);
            // var pngpath = Path.Combine(dir, name + ".png");
            // var png = AssetDatabase.LoadAssetAtPath<Texture2D>(pngpath);
            // if (png == null)
            // {
            //     var pngdir = Path.GetDirectoryName(pngpath);
            //     if (!Directory.Exists(pngdir))
            //     {
            //         Directory.CreateDirectory(pngdir);
            //     }
            //     var pngasset = AssetDatabase.LoadAssetAtPath<Texture2D>(pngpath);
            //     if (pngasset == null)
            //     {
            //         pngasset = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            //         pngasset.SetPixels32(texture.GetPixels32());
            //         pngasset.Apply();
            //         AssetDatabase.CreateAsset(pngasset, pngpath);
            //     }
            //     AssetDatabase.CreateAsset(sprite, toAssetPath);
            // }
            // else
            // {
            //     var spriteasset = AssetDatabase.LoadAssetAtPath<Sprite>(toAssetPath);
            //     if (spriteasset == null)
            //     {
            //         AssetDatabase.CreateAsset(sprite, toAssetPath);
            //     }
            // }
            AssetDatabase.CreateAsset(sprite, toAssetPath);
            return sprite;
        }


    }
}
