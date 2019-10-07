using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class ReferenceResolver : IReferenceResolver
    {
        public bool nextIsSource;
        public JsonStore jsonStore;
        public Queue<IJsonObject> queue;
        public HashSet<IJsonObject> visited;
        public List<DeserializationPair> jObjects;

        public object ResolveReference(object context, string reference)
        {
            return jsonStore.Get(reference);
        }

        public string GetReference(object context, object value)
        {
            if (value is IJsonObject persistent)
            {
                if (!jsonStore.Contains(persistent))
                {
                    jsonStore.Add(persistent);
                }

                return jsonStore.GetOrAddId(persistent);
            }

            return null;
        }

        public bool IsReferenced(object context, object value)
        {
            if (value is IJsonObject persistent)
            {
                if (nextIsSource)
                {
                    nextIsSource = false;
                    return false;
                }

                if (visited.Add(persistent))
                {
                    queue.Enqueue(persistent);
                }

                return true;
            }

            return false;
        }

        public void AddReference(object context, string reference, object value)
        {
        }
    }
}
