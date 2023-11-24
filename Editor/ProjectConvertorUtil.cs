using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity2DAdapter
{
    public static class ProjectConvertorUtil
    {
        // 去掉出其中 -xx 指定的参数（-xx 和 -xx紧跟其后不是以-开头的参数）
        // 去掉第一个可执行文件名参数
        private static string[] GetCommandLineArgs()
        {
            var origin = System.Environment.GetCommandLineArgs();
            var result = new System.Collections.Generic.List<string>();
            for (int i = 1; i < origin.Length; i++)
            {
                var arg = origin[i];
                if (arg.StartsWith("-"))
                {
                    if (i + 1 < origin.Length && !origin[i + 1].StartsWith("-"))
                    {
                        i++;
                    }
                }
                else
                {
                    result.Add(arg);
                }
            }
            return result.ToArray();
        }

        private static void Log(object message)
        {
            // Debug.Log(message);
            System.Console.WriteLine(message);
        }

        public static void Test()
        {

            var a = GetCommandLineArgs();
            Log(string.Join(", ", a));
            throw new System.Exception("Hello World!");
        }

        public static void CocosToUnity()
        {
            var args = GetCommandLineArgs();
            string pathPrefix = args[0];                        //# "D:/book/story_"
            string storyIDs = args.Length > 1 ? args[1] : null; //# "0001,0037"
            var pathList = new List<string>();
            if (storyIDs != null)
            {
                var ids = storyIDs.Split(',');
                foreach (var id in ids)
                {
                    pathList.Add(pathPrefix + id);
                }
            }
            else
            {
                pathList.Add(pathPrefix);
            }
            foreach (var path in pathList)
            {
                // 每次重新创建 Parser 和 Convertor，避免上次的状态影响，也降低潜在的内存溢出风险
                ProjectConvertor.Parser = new CocoStudio.Parser
                {
                    RelativeSrcResPath = "cocosstudio",
                    RelativeExpResPath = "res",
                    IsConvertCSD = true,
                    IsConvertCSI = true,
                };
                ProjectConvertor.Convertor = new Unity.CanvasAnimatedGameObjectConvertor
                {
                    SkipExistPrefab = true,
                    SkipExistSpriteAtlas = true,
                    SkipExistSprite = true,
                };
                var validPaths = Csd2UnityPrefab.EnumValidInputPath(path);
                string OutputPath = "Assets/art/story";

                foreach (var validPath in validPaths)
                {
                    Log($"Convert {validPath} to {OutputPath}");
                    ProjectConvertor.Convert(validPath, OutputPath, false);
                }
            }
        }
    }
}
