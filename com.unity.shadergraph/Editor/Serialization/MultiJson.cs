using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJson
    {
        public static void Deserialize<T>(T objectToOverwrite, string json, JsonObject referenceRoot = null, bool rewriteIds = false) where T : JsonObject
        {
            var entries = MultiJsonInternal.Parse(json);
            if (referenceRoot != null)
            {
                MultiJsonInternal.PopulateValueMap(referenceRoot);
            }
            MultiJsonInternal.Deserialize(objectToOverwrite, entries, rewriteIds);
        }

        public static string Serialize(JsonObject mainObject)
        {
            return MultiJsonInternal.Serialize(mainObject);
        }

        public static Type ParseType(string typeString)
        {
            return MultiJsonInternal.ParseType(typeString);
        }
    }
}
