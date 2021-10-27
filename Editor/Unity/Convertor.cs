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
            throw new NotImplementedException();
        }

        public void ConvertSpriteList(string assetpath, Func<string, SpriteList> GetSpriteList)
        {
            throw new NotImplementedException();
        }

        public void ImportUnparsedAsset(string assetpath, Func<string, string> GetFullPath)
        {
            string fullpath = GetFullPath(assetpath);
            if (fullpath == null)
            {
                Debug.LogError("Can't find the asset path: " + assetpath);
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
                    importedAssets.Add(assetpath, sprite);
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
            importedAssets = new Dictionary<string, UnityEngine.Object>();
        }

        # endregion

        public string OutputPath { get; private set; }
        public string FullOutputPath { get; private set; }
        private Dictionary<string, UnityEngine.Object> importedAssets;

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

        protected Sprite GetSprite(string assetPath)
        {
            importedAssets.TryGetValue(assetPath, out var sprite);
            return sprite as Sprite;
        }

        private SpriteAtlas CreateSpriteAtlas(string fromAssetPath, SpriteList spriteList)
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
            var asset = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(toAssetPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(atlas, toAssetPath);
            }
            AssetDatabase.
            return atlas;
        }
    }
}
