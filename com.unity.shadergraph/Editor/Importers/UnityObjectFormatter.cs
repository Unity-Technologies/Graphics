//using System;
//using System.Collections.Generic;
//using System.Text;
//using UnityEngine;
//using Utf8Json;
//
//namespace UnityEditor.Importers
//{
//    class UnityObjectFormatter<T> : IJsonFormatter<T>
//    {
//        public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
//        {
//            writer.WriteString(JsonUtility.ToJson(value));
//        }
//
//        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
//        {
//            var start = reader.GetCurrentOffsetUnsafe();
//            reader.ReadNextBlock();
//            var end = reader.GetCurrentOffsetUnsafe();
//            var jsonString = Encoding.UTF8.GetString(reader.GetBufferUnsafe(), start, end - start);
//            Debug.Log(typeof(T).FullName + "\n" + jsonString);
//            return JsonUtility.FromJson<T>(jsonString);
//        }
//    }
//}
