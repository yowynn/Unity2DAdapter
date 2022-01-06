using System.Collections.Generic;
using System.IO;
using Unity2DAdapter.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;


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


        [MenuItem("Unity2DAdapter/Image Size Compression")]
        static void CreateWizard()
        {
            var wzd = ImageSizeCompression.DisplayWizard<ImageSizeCompression>("Image Size Compression", "Don't Click!", "Apply");
        }

        [SerializeField, Tooltip("设置图片导入的MaxSize")]
        public CompressMaxSize MaxSize = CompressMaxSize._1024;


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
            var textures = GetObjectsFromSelection<Texture2D>();
            foreach (Texture2D texture in textures)
            {
                SetTextureImporterFormat(texture);
            }
            var atlases = GetObjectsFromSelection<SpriteAtlas>();
            foreach (SpriteAtlas atlas in atlases)
            {
                SetSpriteAtlasFormat(atlas);
            }
        }

        private Object[] GetObjectsFromSelection<T>()
        {
            var objects = Selection.GetFiltered(typeof(T), SelectionMode.DeepAssets);
            return objects;
        }

        private void SetTextureImporterFormat(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.maxTextureSize > (int)MaxSize)
            {
                settings.maxTextureSize = (int)MaxSize;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                Debug.Log("Set TextureImporterFormat: " + path);
            }
        }

        private void SetSpriteAtlasFormat(SpriteAtlas atlas)
        {
            TextureImporterPlatformSettings settings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            if (settings.maxTextureSize > (int)MaxSize)
            {
                settings.maxTextureSize = (int)MaxSize;
                atlas.SetPlatformSettings(settings);
                // AssetDatabase.CreateAsset(atlas, AssetDatabase.GetAssetPath(atlas));
                AssetDatabase.SaveAssets();
                Debug.Log("Set SpriteAtlasFormat: " + atlas.name);
            }
        }
    }
}
