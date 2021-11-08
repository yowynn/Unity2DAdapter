
using System;
using System.Text;
using System.IO;

namespace Unity2DAdapter.Util
{
    public static class ProcessLog
    {
        private static StringBuilder sb = new StringBuilder();
        public static event Action<string> ErrorHandler;
        public static event Action<string> InfoHandler;
        public static void Log(string log)
        {
            var time = DateTime.Now.ToLongTimeString();
            var msg = string.Format("[{0}] {1}", time, log);
            sb.AppendLine(msg);
            if (InfoHandler != null) InfoHandler(msg);
        }

        public static void LogError(string log)
        {
            var time = DateTime.Now.ToLongTimeString();
            var msg = string.Format("[{0}] ERROR: {1}", time, log);
            sb.AppendLine(msg);
            if (ErrorHandler != null) ErrorHandler(msg);
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
