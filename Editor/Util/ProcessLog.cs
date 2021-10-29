
using System;
using System.Text;
using System.IO;

namespace Cocos2Unity.Util
{
    public static class ProcessLog
    {
        static StringBuilder sb = new StringBuilder();
        public static void Log(string log)
        {
            var time = DateTime.Now.ToLongTimeString();
            sb.AppendLine(string.Format("[{0}] {1}", time, log));
        }

        public static void LogError(string log)
        {
            var time = DateTime.Now.ToLongTimeString();
            sb.AppendLine(string.Format("[{0}] ERROR: {1}", time, log));
        }
        public static string Flush(string logfile = null)
        {
            if (sb.Length > 0)
            {
                string s = sb.ToString();
                if (!string.IsNullOrEmpty(logfile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logfile));
                    System.IO.File.AppendAllText(logfile, s);
                }
                sb.Length = 0;
                return s;
            }
            return "";
        }
    }
}
