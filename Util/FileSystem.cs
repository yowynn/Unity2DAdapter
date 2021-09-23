using System;
using System.IO;

namespace Wynncs.Util
{
    public class FileSystem
    {
        /// <summary>
        /// 遍历一个路径（文件或文件夹）
        /// </summary>
        /// <param name="fullPath">完整路径</param>
        /// <param name="onInfo">回调</param>
        /// <param name="recursive">是否递归遍历子文件夹</param>
        public static void EnumPath(string fullPath, Action<FileSystemInfo> onInfo, bool recursive = true)
        {
            if (Directory.Exists(fullPath))
            {
                var info = new DirectoryInfo(fullPath);
                onInfo(info);
                if (recursive)
                {
                    foreach (var folder in info.GetDirectories())
                    {
                        EnumPath(folder.FullName, onInfo, recursive);
                    }
                }
                foreach (var file in info.GetFiles())
                {
                    onInfo(file);
                }
            }
            else if (File.Exists(fullPath))
            {
                var info = new FileInfo(fullPath);
                onInfo(info);
            }
        }

        /// <summary>
        /// 获取路径信息
        /// </summary>
        /// <param name="fullPath">完整路径</param>
        /// <returns>路径信息</returns>
        public static FileSystemInfo GetPathInfo(string fullPath)
        {
            if (Directory.Exists(fullPath))
            {
                return new DirectoryInfo(fullPath);
            }
            else if (File.Exists(fullPath))
            {
                return new FileInfo(fullPath);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取路径所在的文件夹路径
        /// </summary>
        /// <param name="fullPath">完整路径</param>
        /// <returns>文件夹路径</returns>
        public static string GetFolderPath(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                return Path.GetFullPath(Path.GetDirectoryName(fullPath));
            }
            else if (Directory.Exists(fullPath))
            {
                return Path.GetFullPath(fullPath);
            }
            else if (Path.HasExtension(fullPath))
            {
                return Path.GetFullPath(Path.GetDirectoryName(fullPath));
            }
            else
            {
                return Path.GetFullPath(fullPath);
            }
        }

        /// <summary>
        /// 判断文件系统信息是否表示文件夹
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IsFolder(FileSystemInfo info)
        {
            return info as DirectoryInfo != null;
        }

        /// <summary>
        /// 判断文件系统信息是否表示文件
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IsFile(FileSystemInfo info)
        {
            return info as FileInfo != null;
        }

        /// <summary>
        /// 把文件系统信息当作文件信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static FileInfo AsFileInfo(FileSystemInfo info)
        {
            return info as FileInfo;
        }

        /// <summary>
        /// 把文件系统信息当作文件夹信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public DirectoryInfo AsFolder(FileSystemInfo info)
        {
            return info as DirectoryInfo;
        }

        /// <summary>
        /// DEBUG: 打印文件名信息
        /// </summary>
        public static Action<FileSystemInfo> ShowBaseInfo = info => Console.WriteLine($"{info.FullName} -- {IsFolder(info)}");
    }
}
