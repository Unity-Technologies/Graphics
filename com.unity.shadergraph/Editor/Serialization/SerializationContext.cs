using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityEditor.ShaderGraph.Serialization
{
    class SerializationContext : IDisposable
    {
        static ThreadLocal<SerializationContext> s_Instance = new ThreadLocal<SerializationContext>(() => new SerializationContext());

        JsonStore m_JsonStore;
        Queue<JsonObject> m_Queue = new Queue<JsonObject>();
        HashSet<JsonObject> m_Visited = new HashSet<JsonObject>();

        public Queue<JsonObject> queue => m_Queue;

        public HashSet<JsonObject> visited => m_Visited;

        public static string GetReference(JsonObject jsonObject)
        {
            var context = s_Instance.Value;
            if (context.visited.Add(jsonObject))
            {
                context.queue.Enqueue(jsonObject);
            }

            return jsonObject.jsonId;
        }

        public static SerializationContext Begin(JsonStore jsonStore)
        {
            var context = s_Instance.Value;
            if (context.m_JsonStore != null)
            {
                throw new InvalidOperationException("Recursive serialization of JsonStore is not allowed.");
            }

            context.m_JsonStore = jsonStore;
            context.visited.Add(jsonStore.root);
            return context;
        }

        public void Dispose()
        {
            m_JsonStore = null;
            visited.Clear();
            queue.Clear();
        }
    }
}
