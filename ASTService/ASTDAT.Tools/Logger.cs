using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Tools
{
    public class Logger
    {
        private static object lockObject = new object();

        public static bool Formating { get; set; } = true;
        public static string FileName { get; set; } = "Log.txt";
        public static string Folder { get; set; } = "";
        public static bool Enabled { get; set; } = true;

        static Logger()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            Folder = Path.GetDirectoryName(path);
        }

        public static void Write(string str, string level = "I", string fileName = null)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                if (Formating)
                {
                    str = $"[{level}][{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {str}";
                }
                lock (lockObject)
                {
                    fileName = string.IsNullOrWhiteSpace(fileName) ? FileName : fileName;
                    System.IO.File.AppendAllText($"{Folder}\\{fileName}", $"{str}\r\n");
                }
            }
            catch
            {
                
            }
        }

        public static void WriteError(params string[] str)
        {
            Write(String.Join(",", str), "E");
        }

        public static void Write(string str, Exception exc, string fileName = null)
        {
            str = $"{str} Exception:{exc.Message}, InnerException:{(exc.InnerException == null ? "" : exc.InnerException.Message)},StackTrace:{exc.StackTrace}";
            Write(str, "E", fileName);
        }
    }
}
