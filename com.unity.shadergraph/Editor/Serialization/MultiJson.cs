using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJson
    {
        public static T Deserialize<T>(string json, bool rewriteIds = false) where T : JsonObject
        {
            var entries = MultiJsonInternal.Parse(json, typeof(T).FullName);
            return (T)MultiJsonInternal.Deserialize(entries, rewriteIds);
        }

        public static string Serialize(JsonObject mainObject)
        {
            return MultiJsonInternal.Serialize(mainObject);
        }
    }
}
