
using System;
using System.Text;
using System.IO;

namespace Cocos2Unity.Models
{
    public static class ProcessLog
    {
        static StringBuilder sb = new StringBuilder();
        public static void Log(string log)
        {
            var time = DateTime.Now.ToLongTimeString();
            sb.AppendLine(string.Format("[{0}] {1}", time, log));
            Flush();  //for debug
        }
        public static void Flush(string logfile = null)
        {
            if (sb.Length > 0)
            {
                string s = sb.ToString();
                if (string.IsNullOrEmpty(logfile))
                {
                    UnityEngine.Debug.Log(s);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logfile));
                    System.IO.File.AppendAllText(logfile, s);
                }
                sb.Length = 0;
            }
        }
    }
}
