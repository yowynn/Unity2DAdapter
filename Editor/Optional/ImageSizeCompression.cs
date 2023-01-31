using System.Collections.Generic;
using System.IO;
using Unity2DAdapter.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using Spine;


namespace Unity2DAdapter.Optional
{
    public class ImageSizeCompression : ScriptableWizard
    {
        public enum CompressMaxSize
        {
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192,
        }


        [MenuItem("DevTool/Unity2DAdapter/Image Size Compression")]
        static void CreateWizard()
        {
            var wzd = ImageSizeCompression.DisplayWizard<ImageSizeCompression>("Image Size Compression", "Don't Click!", "Apply");
        }

        [SerializeField, Tooltip("设置图片导入的MaxSize")]
        public CompressMaxSize MaxSize = CompressMaxSize._1024;

        [SerializeField, Tooltip("是否处理 Texture2D")]
        public bool HandleTexture2Ds = false;

        [SerializeField, Tooltip("是否处理 SpriteAtlas")]
        public bool HandleSpriteAtlases = true;


        void OnWizardCreate()
        {
            // DO NOTHING
        }

        void OnWizardUpdate()
        {
            helpString = @"Image Size Compression";
        }

        // When the user presses the "Apply" button OnWizardOtherButton is called.
        void OnWizardOtherButton()
        {
            if (HandleTexture2Ds)
            {
                var textures = GetAllAssetPathsFromSelection<Texture2D>();
                foreach (string texture in textures)
                {
                    SetTextureImporterFormat(texture);
                }
                Debug.Log($"Handle {textures.Length} Texture2Ds~");
            }
            if (HandleSpriteAtlases)
            {
                var atlases = GetAllAssetPathsFromSelection<SpriteAtlas>();
                foreach (string atlas in atlases)
                {
                    SetSpriteAtlasFormat(atlas);
                }
                Debug.Log($"Handle {atlases.Length} SpriteAtlases~");
            }
        }

        private string[] GetAllAssetPathsFromSelection<T>()
        {
            var type = typeof(T);
            var selectGuids = Selection.assetGUIDs;
            var paths = new string[selectGuids.Length];
            for (var i = 0; i < selectGuids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(selectGuids[i]);
            }
            var founds = AssetDatabase.FindAssets($"t:{type.Name}", paths);
            var assetPaths = new string[founds.Length];
            for(var i = 0; i < assetPaths.Length; i++)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(founds[i]);
            }
            return assetPaths;
        }

        private void SetTextureImporterFormat(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(path);
                return;
            }
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.maxTextureSize > (int)MaxSize)
            {
                settings.maxTextureSize = (int)MaxSize;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                Debug.Log("Set TextureImporterFormat: " + path);
            }
        }

        private void SetSpriteAtlasFormat(string path)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                Debug.LogError(path);
                return;
            }
            TextureImporterPlatformSettings settings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            if (settings.maxTextureSize > (int)MaxSize)
            {
                settings.maxTextureSize = (int)MaxSize;
                atlas.SetPlatformSettings(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("Set SpriteAtlasFormat: " + atlas.name);
            }
        }
    }
}
