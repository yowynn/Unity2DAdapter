using System;

namespace Cocos2Unity
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args.Length > 0 ? args[0] : null;
            var fs = new Wynnsharp.FileSystemUtil();
            var xml = new Wynnsharp.XmlUtil();

            path = "C:/Users/Wynn/Desktop/book";
            fs.EnumFolder(path, f =>
            {
                if (!fs.IsFolder(f) && f.Extension.ToLower() == ".csd")
                {
                    var srcfile = f.FullName;
                    var logfile = "out/log.xml";
                    xml.Statistics(srcfile, logfile, true);
                    Console.WriteLine(f.FullName);

                }
            });
            Console.WriteLine("Hello World!");
            Console.WriteLine(path);
            Console.Read();
        }
    }
}
