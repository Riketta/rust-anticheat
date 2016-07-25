using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LitJson;

namespace RowAC
{
    class RConfig<T>
    {
        public static string ToJson(T obj)
        {
            return JsonMapper.ToJson(obj);
        }

        public static T ReadFromFile(string file)
        {
            T ret = default(T);
            using (StreamReader reader = new StreamReader(file))
            {
                string json = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(json))
                    ret = JsonMapper.ToObject<T>(json);
            }
            return ret;
        }

        public static void WriteToFile(T obj, string file)
        {
            using (StreamWriter writer = new StreamWriter(file))
                writer.Write(ToJson(obj));
        }
    }
}
