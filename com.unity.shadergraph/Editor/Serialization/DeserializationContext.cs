using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class DeserializationContext : IDisposable
    {
        static ThreadLocal<DeserializationContext> s_Instance = new ThreadLocal<DeserializationContext>(() => new DeserializationContext());

        Dictionary<string, JsonObject> m_ObjectMap;
        List<(JsonObject, string)> m_Queue = new List<(JsonObject, string)>();

        public List<(JsonObject, string)> queue => m_Queue;

        public static DeserializationContext Begin(Dictionary<string, JsonObject> objectMap)
        {
            var context = s_Instance.Value;
            if (context.m_ObjectMap != null)
            {
                throw new InvalidOperationException("Recursive deserialization of JsonAsset is not allowed.");
            }

            context.m_ObjectMap = objectMap;
            return context;
        }

        public static JsonObject ResolveReference(string reference)
        {
            var context = s_Instance.Value;
            if (context.m_ObjectMap == null)
            {
                throw new InvalidOperationException("JsonRef can only be deserialized in the context of a JsonStore.");
            }

            if (context.m_ObjectMap.TryGetValue(reference, out var jsonObject))
            {
                return jsonObject;
            }

            return null;
        }

        public static void Enqueue(JsonObject instance, string json)
        {
            s_Instance.Value.queue.Add((instance, json));
        }

        public void Dispose()
        {
            m_ObjectMap = null;
            queue.Clear();
        }
    }
}
