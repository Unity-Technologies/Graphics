using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class SerializationExtensions
    {
        public static RefValueEnumerable<T> SelectValue<T>(this List<JsonRef<T>> list) where T : JsonObject =>
            new RefValueEnumerable<T>(list);

        public static DataValueEnumerable<T> SelectValue<T>(this List<JsonData<T>> list) where T : JsonObject =>
            new DataValueEnumerable<T>(list);

        public static void AddRange<T>(this List<JsonRef<T>> list, IEnumerable<T> enumerable)
            where T : JsonObject
        {
            foreach (var jsonObject in enumerable)
            {
                list.Add(jsonObject);
            }
        }

        public static void AddRange<T>(this List<JsonRef<T>> list, List<T> enumerable)
            where T : JsonObject
        {
            foreach (var jsonObject in enumerable)
            {
                list.Add(jsonObject);
            }
        }

        public static void AddRange<T>(this List<T> list, List<JsonRef<T>> enumerable)
            where T : JsonObject
        {
            foreach (var jsonObject in enumerable)
            {
                list.Add(jsonObject);
            }
        }
    }
}
