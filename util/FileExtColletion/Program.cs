using System;
using System.IO;
using System.Collections.Generic;

namespace FileExtColletion
{
    class Program
    {
        public delegate void OnInfo(FileSystemInfo info);
        static void EnumFile(string path, OnInfo onInfo)
        {
            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                onInfo(info);
                foreach (var folder in info.GetDirectories())
                {
                    EnumFile(folder.FullName, onInfo);
                }
                foreach (var file in info.GetFiles())
                {
                    onInfo(file);
                }
            }
        }

        static bool IsFolder(FileSystemInfo info)
        {
            return info as DirectoryInfo != null;
        }

        static void ShowInfo(FileSystemInfo info)
        {
            Console.WriteLine($"{ info.FullName } -- { IsFolder(info) }");
        }
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string path = args[0];
                var map = new Dictionary<string, int>();
                EnumFile(path, f =>
                {
                    if (!IsFolder(f))
                    {
                        var ext = f.Extension;
                        int count;
                        map[ext] = map.TryGetValue(ext, out count) ? count + 1 : 1;
                    }
                });
                foreach(var item in map)
                {
                    Console.WriteLine($"{ item.Key } -- { item.Value }");
                }
            }
            // Console.WriteLine("Hello World!");
            Console.Read();
        }
    }
}
