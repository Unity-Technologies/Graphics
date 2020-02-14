using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJson
    {
        public static T Deserialize<T>(string json) where T : JsonObject
        {
            var entries = MultiJsonInternal.Parse(json);
            return (T)MultiJsonInternal.Deserialize(entries);
        }
    }
}
