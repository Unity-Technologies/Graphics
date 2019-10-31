using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class DeserializationContext : IDisposable
    {
        static ThreadLocal<DeserializationContext> s_Instance = new ThreadLocal<DeserializationContext>(() => new DeserializationContext());

        JsonStore m_JsonStore;
        List<(JsonObject, string)> m_Queue = new List<(JsonObject, string)>();

        public List<(JsonObject, string)> queue => m_Queue;

        public static DeserializationContext Begin(JsonStore jsonStore)
        {
            var context = s_Instance.Value;
            if (context.m_JsonStore != null)
            {
                throw new InvalidOperationException("Recursive deserialization of JsonStore is not allowed.");
            }

            context.m_JsonStore = jsonStore;
            return context;
        }

        public static JsonObject ResolveReference(string reference)
        {
            var context = s_Instance.Value;
            if (context.m_JsonStore == null)
            {
                throw new InvalidOperationException("JsonRef can only be deserialized in the context of a JsonStore.");
            }
            return context.m_JsonStore.Get(reference);
        }

        public static void Enqueue(JsonObject instance, string json)
        {
            s_Instance.Value.queue.Add((instance, json));
        }

        public void Dispose()
        {
            m_JsonStore = null;
            queue.Clear();
        }
    }
}
