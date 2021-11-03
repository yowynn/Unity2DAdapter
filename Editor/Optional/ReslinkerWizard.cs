using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using iHuman.Ams.Plugin;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Cocos2Unity.Optional
{
    public class ReslinkerWizard : ScriptableWizard
    {
        #region base

        [SerializeField, Tooltip("Input Path, A File or A Folder.")]
        private string inputPath = "D:\\cocosStudio\\Assets\\ams\\plugin\\cocostudio\\Editor\\Optional\\input";
        
        [SerializeField, Tooltip("Asset Path, A Folder contains the assets to be found.")]
        private string assetPath = "D:\\cocosStudio\\Assets\\art\\story";

        [SerializeField, Tooltip("Output Path, A Folder. Must be in the asset folder!")]
        private string outputPath = "D:\\cocosStudio\\Assets\\ams\\plugin\\cocostudio\\Editor\\Optional\\output";
        
        [SerializeField, Tooltip("Name of the ReslinkAsset generated")]
        private string resName = "res";

        private List<FileItem> res = new List<FileItem>();

        [MenuItem("ToUnity2D/Reslinker")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<ReslinkerWizard>("Link Res Used in Project JSON", "Create", "Apply");
        }

        void OnWizardCreate()
        {
            inputPath = inputPath.Replace("/", "\\");
            outputPath = outputPath.Replace("/", "\\");
            GetFiles(inputPath);
            bool createValid = Directory.Exists(inputPath) && Directory.Exists(outputPath);
            if (createValid) GenerateReslink();
            else throw new ErrorException("Invalid file path!");
        }

        void OnWizardUpdate()
        {
            helpString = @"Convert '.json' files to reslinkAsset";
        }

        // When the user presses the "Apply" button OnWizardOtherButton is called.
        void OnWizardOtherButton()
        {
            this.Close();
        }

        #endregion

        #region generate reslink

        void GenerateReslink()
        {
            string outputFile = string.Concat(outputPath, "\\", resName , ".asset");
            outputFile = FormatAssetPath(outputFile);
            outputFile = SetOutputPath(outputFile);
            ResLinkAsset resLinkAsset = ScriptableObject.CreateInstance<ResLinkAsset>();
            SortListBySuffix();
            foreach (var fileItem in res)
            {
                ResLinkAsset.Item item = new ResLinkAsset.Item();
                item.Key = fileItem.fielName;
                item.Val = GenerateAseetObject(fileItem.fielName);
                resLinkAsset.assets.Add(item);
            }
            AssetDatabase.CreateAsset(resLinkAsset, outputFile);
        }

        UnityEngine.Object GenerateAseetObject(string filePath)
        {
            Object obj = null;
            string fullpath = $"{assetPath}/{filePath}";
            fullpath = fullpath.Replace("//", "/");
            fullpath = fullpath.Replace(@"\", "/");
            if (!File.Exists(fullpath)) return obj;
            else
            {
                FileInfo fileInfo = new FileInfo(fullpath);
                int index = fullpath.IndexOf("Assets/");
                string unityPath = fullpath.Substring(index);
                obj = AssetDatabase.LoadAssetAtPath<Object>(unityPath);
                return obj;
            }
        }

        void SortListBySuffix()
        {
            res.Sort(delegate(FileItem item, FileItem fileItem)
            {
                if (item.tag.Length > fileItem.tag.Length)
                {
                    return 1;
                }
                if (item.tag.Length < fileItem.tag.Length)
                {
                    return -1;
                }
                
                int len = item.tag.Length;
                int[] sortNum = new int[len];
                int res = 0;
                for (int i = 0; i < sortNum.Length; ++i)
                {
                    sortNum[i] = item.tag[i] - fileItem.tag[i];
                    res += sortNum[i];
                }

                return res;
            });
        }

        private string FormatAssetPath(string filePath)
        {
            var newFilePath1 = filePath.Replace("\\", "/");
            var newFilePath2 = newFilePath1.Replace("//", "/").Trim();
            newFilePath2 = newFilePath2.Replace("///", "/").Trim();
            newFilePath2 = newFilePath2.Replace("\\\\", "/").Trim();
            return newFilePath2;
        }
        
        private string SetOutputPath(string path)
        {
            string output = "";
            path = Path.GetFullPath(path).Replace("\\", "/"); ;
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            var assetRoot = Application.dataPath.Replace('\\', '/');
            if (path.StartsWith(assetRoot))
            {
                output = path.Replace(assetRoot, "Assets");
            }
            else
            {
                throw new Exception("Output path must be in Assets folder");
            }

            return output;
        }

        #endregion

        #region get files

        void GetFiles(string path)
        {
            if (File.Exists(path))
            {
                // todo if inputpath is a file.
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo root = new DirectoryInfo(path);
                DirectoryInfo[] directoryInfo = root.GetDirectories();
                for (int i = 0; i < directoryInfo.Length; ++i)
                {
                    string filePath = string.Concat(path, "\\", directoryInfo[i].Name);
                    GetFiles(filePath);
                }

                FileInfo[] files = root.GetFiles("*");
                for (int i = 0; i < files.Length; ++i)
                {
                    if (files[i].Name.EndsWith(".meta"))
                    {
                        continue;
                    }

                    try
                    {
                        string jsonPath = string.Concat(path, "\\", files[i].Name);
                        string jsonData = JsonParser.LoadJsonFromFile(jsonPath);
                        GetFileNames(jsonData);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
            else
            {
                throw new ErrorException("Invalid file path!");
            }
        }

        void GetFileNames(string jsonString)
        {
            int colonCount = 0;
            for (int i = 0; i < jsonString.Length; ++i)
            {
                if (jsonString[i].Equals('"') && colonCount % 2 == 0)
                {
                    int endIdx = 0;
                    colonCount += 1;
                    if (i == jsonString.Length - 1) break;
                    for (int j = i + 1; j < jsonString.Length; ++j)
                    {
                        if (jsonString[j].Equals('"'))
                        {
                            colonCount += 1;
                            endIdx = j;
                            string tmpString = jsonString.Substring(i + 1, endIdx - i - 1);
                            Regex regex = new Regex(@"\b^.+?\..+$\b");
                            if (regex.IsMatch(tmpString))
                            {
                                int idx = tmpString.IndexOf('.');
                                string tag = tmpString.Substring(idx+1);
                                FileItem fileItem = new FileItem();
                                fileItem.fielName = tmpString;
                                fileItem.tag = tag;
                                res.Add(fileItem);
                            }
                            i = j;
                            break;
                        }
                    }
                }
            }
        }

        #endregion
    }

    public class FileItemComparer : IComparer<FileItem>
    {
        public int Compare(FileItem fileItem_1, FileItem fileItem_2)
        {
            char c_1 = fileItem_1.tag[0];
            char c_2 = fileItem_2.tag[0];
            return c_1 - c_2;
        }
    }

    public class FileItem
    {
        public string fielName;
        public string tag = "a";
    }

    public class ErrorException : Exception
    {
        private string errMsg;

        public ErrorException(string msg)
        {
            errMsg = msg;
            Debug.LogError(errMsg);
        }
    }
}
