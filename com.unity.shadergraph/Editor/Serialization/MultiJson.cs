using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJson
    {
        public static void Deserialize<T>(T objectToOverwrite, string json) where T : JsonObject
        {
            var entries = MultiJsonInternal.Parse(json);
            MultiJsonInternal.Deserialize(objectToOverwrite, entries, false);
        }

        public static void Deserialize<T>(T objectToOverwrite, string json, JsonObject referenceRoot) where T : JsonObject
        {
            var entries = MultiJsonInternal.Parse(json);
            MultiJsonInternal.PopulateValueMap(referenceRoot);
            MultiJsonInternal.Deserialize(objectToOverwrite, entries, true);
        }

        public static string Serialize(JsonObject mainObject)
        {
            return MultiJsonInternal.Serialize(mainObject);
        }
    }
}
