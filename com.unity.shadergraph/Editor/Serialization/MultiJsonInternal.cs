using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJsonInternal
    {
        internal static readonly Dictionary<string, Type> typeMap = CreateTypeMap();

        internal static bool isDeserializing;

        internal static readonly Dictionary<string, JsonObject> instanceMap = new Dictionary<string, JsonObject>();

        internal static bool isSerializing;

        internal static readonly List<JsonObject> serializationQueue = new List<JsonObject>();

        internal static readonly HashSet<string> serializedSet = new HashSet<string>();

        static Dictionary<string, Type> CreateTypeMap()
        {
            var map = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<JsonObject>())
            {
                if (type.FullName != null)
                {
                    map[type.FullName] = type;
                }
            }

            foreach (var type in TypeCache.GetTypesWithAttribute(typeof(FormerNameAttribute)))
            {
                if (type.IsAbstract || !typeof(JsonObject).IsAssignableFrom(type))
                {
                    continue;
                }

                foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                {
                    var legacyAttribute = (FormerNameAttribute)attribute;
                    map[legacyAttribute.fullName] = type;
                }
            }

            return map;
        }

        public static List<MultiJsonEntry> Parse(string str)
        {
            var result = new List<MultiJsonEntry>();
            var separatorStr = $"\n\n";
            var startIndex = 0;
            var raw = new FakeScriptableObject<FakeJsonObject>();

            while (startIndex < str.Length)
            {
                var jsonBegin = startIndex;
                var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
                if (jsonEnd == -1)
                {
                    jsonEnd = str.Length;
                }

                var json = str.Substring(jsonBegin, jsonEnd - jsonBegin);
                JsonUtility.FromJsonOverwrite(json, raw);
                result.Add(new MultiJsonEntry(raw.MonoBehaviour.type, raw.MonoBehaviour.id, json));
                raw.MonoBehaviour.Reset();

                startIndex = jsonEnd + separatorStr.Length;
            }

            return result;
        }

        public static JsonObject Deserialize(List<MultiJsonEntry> entries)
        {
            if (isDeserializing)
            {
                throw new InvalidOperationException("Nested MultiJson deserialization is not supported.");
            }

            try
            {
                isDeserializing = true;

                foreach (var entry in entries)
                {
                    if (!typeMap.TryGetValue(entry.type, out var type))
                    {
                        // TODO: Implement fallback types that preserves JSON
                        continue;
                    }

                    try
                    {
                        var instance = (JsonObject)Activator.CreateInstance(type);
                        instanceMap[entry.id] = instance;
                    }
                    catch (Exception e)
                    {
                        // External code could throw exceptions, but we don't want that to fail the whole thing.
                        // Potentially, the fallback type should also be used here.
                        // TODO: Allow custom logging function
                        Debug.LogException(e);
                    }
                }

                foreach (var entry in entries)
                {
                    try
                    {
                        var instance = instanceMap[entry.id];
                        var fakeType = typeof(FakeScriptableObject<>).MakeGenericType(instance.GetType());
                        var fake = Activator.CreateInstance(fakeType, instance);
                        EditorJsonUtility.FromJsonOverwrite(entry.json, fake);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                foreach (var entry in entries)
                {
                    try
                    {
                        var instance = instanceMap[entry.id];
                        instance.OnAfterMultiDeserialize(entry.json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                return instanceMap[entries[0].id];
            }
            finally
            {
                instanceMap.Clear();
                isDeserializing = false;
            }
        }

        public static string Serialize(JsonObject mainObject)
        {
            if (isSerializing)
            {
                throw new InvalidOperationException("Nested MultiJson serialization is not supported.");
            }

            try
            {
                isSerializing = true;

                serializedSet.Add(mainObject.id);
                serializationQueue.Add(mainObject);

                var idJsonList = new List<(string, string)>();

                // Not a foreach because the queue is populated by `JsonRef<T>`s as we go.
                for (var i = 0; i < serializationQueue.Count; i++)
                {
                    var instance = serializationQueue[i];
                    var fakeType = typeof(FakeScriptableObject<>).MakeGenericType(instance.GetType());
                    var fake = Activator.CreateInstance(fakeType, instance);
                    var json = EditorJsonUtility.ToJson(fake, true);
                    idJsonList.Add((instance.id, json));
                }

                idJsonList.Sort((x, y) =>
                    // Main object needs to be placed first
                    x.Item1 == mainObject.id ? -1 :
                    y.Item1 == mainObject.id ? 1 :
                    // We sort everything else by ID to persistently maintain positions in the output
                    x.Item1.CompareTo(y.Item1));

                var sb = new StringBuilder();
                foreach (var (_, json) in idJsonList)
                {
                    sb.AppendLine(json);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            finally
            {
                serializationQueue.Clear();
                serializedSet.Clear();
                isSerializing = false;
            }
        }
    }
}
