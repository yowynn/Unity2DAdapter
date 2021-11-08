using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Spine;

namespace Unity2DAdapter.Optional
{
    public class JsonParser
    {
        public static string LoadJsonFromFile(string path)
        {
            BinaryFormatter bf = new BinaryFormatter();

            if (!File.Exists(path))
            {
                return null;
            }

            StreamReader sr = new StreamReader(path);

            if (sr == null)
            {
                return null;
            }

            string json = sr.ReadToEnd();

            if (json.Length > 0)
            {
                return json;
            }

            return null;
        }
    }
}
