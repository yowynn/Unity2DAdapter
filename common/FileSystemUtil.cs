using System;
using System.IO;
using System.Collections.Generic;

namespace Wynnsharp
{
    public class FileSystemUtil
    {
        public delegate void OnFileSystemInfo(FileSystemInfo info);

        public void EnumFolder(string path, OnFileSystemInfo onInfo)
        {
            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                onInfo(info);
                foreach (var folder in info.GetDirectories())
                {
                    EnumFolder(folder.FullName, onInfo);
                }
                foreach (var file in info.GetFiles())
                {
                    onInfo(file);
                }
            }
            else if (File.Exists(path))
            {
                var info = new FileInfo(path);
                onInfo(info);
            }
            else
            {
                throw new Exception("bad file path");
            }
        }

        public string GetPath(string path)
        {
            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                return info.FullName;
            }
            else if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return Path.GetDirectoryName(info.FullName);
            }
            else
            {
                throw new Exception("bad file path");
            }
        }

        public bool IsFolder(FileSystemInfo info)
        {
            return info as DirectoryInfo != null;
        }
        public bool IsFile(FileSystemInfo info)
        {
            return info as FileInfo != null;
        }

        public DirectoryInfo AsFolder(FileSystemInfo info)
        {
            return info as DirectoryInfo;
        }
        public FileInfo AsFile(FileSystemInfo info)
        {
            return info as FileInfo;
        }

        public void ShowInfo(FileSystemInfo info)
        {
            Console.WriteLine($"{ info.FullName } -- { IsFolder(info) }");
        }

        public Dictionary<string, int> ShowExtensionMap(string path)
        {
            var map = new Dictionary<string, int>();
            EnumFolder(path, f =>
            {
                if (!IsFolder(f))
                {
                    var ext = f.Extension;
                    int count;
                    map[ext] = map.TryGetValue(ext, out count) ? count + 1 : 1;
                }
            });
            // foreach(var item in map)
            // {
            //     Console.WriteLine($"{ item.Key } -- { item.Value }");
            // }
            return map;
        }
    }
}
