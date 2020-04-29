using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class MultiJsonInternal
    {
        static readonly Dictionary<string, Type> k_TypeMap = CreateTypeMap();

        internal static bool isDeserializing;

        internal static readonly Dictionary<string, JsonObject> valueMap = new Dictionary<string, JsonObject>();

        static List<MultiJsonEntry> s_Entries;

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

        public static Type ParseType(string typeString)
        {
            k_TypeMap.TryGetValue(typeString, out var type);
            return type;
        }

        public static List<MultiJsonEntry> Parse(string str)
        {
            var result = new List<MultiJsonEntry>();
            const string separatorStr = "\n\n";
            var startIndex = 0;
            var raw = new FakeJsonObject();

            while (startIndex < str.Length)
            {
                var jsonBegin = str.IndexOf("{", startIndex, StringComparison.Ordinal);
                if (jsonBegin == -1)
                {
                    break;
                }

                var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
                if (jsonEnd == -1)
                {
                    jsonEnd = str.IndexOf("\n\r\n", jsonBegin, StringComparison.Ordinal);
                    if (jsonEnd == -1)
                    {
                        jsonEnd = str.LastIndexOf("}", StringComparison.Ordinal) + 1;
                    }
                }

                var json = str.Substring(jsonBegin, jsonEnd - jsonBegin);

                JsonUtility.FromJsonOverwrite(json, raw);
                if (startIndex != 0 && string.IsNullOrWhiteSpace(raw.type))
                {
                    throw new InvalidOperationException($"Type is null or whitespace in JSON:\n{json}");
                }

                result.Add(new MultiJsonEntry(raw.type, raw.id, json));
                raw.Reset();

                startIndex = jsonEnd + separatorStr.Length;
            }

            return result;
        }

        public static void Enqueue(JsonObject jsonObject, string json)
        {
            if (s_Entries == null)
            {
                throw new InvalidOperationException("Can only Enqueue during JsonObject.OnAfterDeserialize.");
            }

            valueMap.Add(jsonObject.objectId, jsonObject);
            s_Entries.Add(new MultiJsonEntry(jsonObject.GetType().FullName, jsonObject.objectId, json));
        }

        public static bool CreateInstance(string typeString, out JsonObject output)
        {
            output = null;
            if (!k_TypeMap.TryGetValue(typeString, out var type))
            {
                output = new UnknownTypeNode();
                return false;
            }
            output = (JsonObject)Activator.CreateInstance(type, true);
            return true; 
        }

        private static FieldInfo s_ObjectIdField =
            typeof(JsonObject).GetField("m_ObjectId", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Deserialize(JsonObject root, List<MultiJsonEntry> entries, bool rewriteIds)
        {
            if (isDeserializing)
            {
                throw new InvalidOperationException("Nested MultiJson deserialization is not supported.");
            }

            try
            {
                isDeserializing = true;

                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    try
                    {
                        JsonObject value = null;
                        bool tryDeserialize = true;
                        if(index == 0)
                        {
                            value = root;
                        }
                        else
                        {
                            tryDeserialize = CreateInstance(entry.type, out value);
                        }

                        var id = entry.id;

                        if (id != null)
                        {
                            // Need to make sure that references looking for the old ID will find it in spite of
                            // ID rewriting.
                            valueMap[id] = value;
                        }

                        if (rewriteIds || entry.id == null)
                        {
                            id = value.objectId;
                            entries[index] = new MultiJsonEntry(entry.type, id, entry.json);
                            valueMap[id] = value;
                        }

                        s_ObjectIdField.SetValue(value, id);
                    }
                    catch (Exception e)
                    {
                        // External code could throw exceptions, but we don't want that to fail the whole thing.
                        // Potentially, the fallback type should also be used here.
                        Debug.LogException(e);
                    }
                }

                s_Entries = entries;

                // Not a foreach because `entries` can be populated by calls to `Enqueue` as we go.
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    try
                    {
                        var value = valueMap[entry.id];
                        value.Deserailize(entry.type, entry.json);
                        // Set ID again as it could be overwritten from JSON.
                        s_ObjectIdField.SetValue(value, entry.id);
                        value.OnAfterDeserialize(entry.json);
                    }
                    catch (Exception e)
                    {
                        if(!String.IsNullOrEmpty(entry.id))
                        {
                            var value = valueMap[entry.id];
                            if(value != null)
                            {
                                Debug.LogError($"Exception thrown while deserialize object of type {entry.type}: {e.Message}");
                            }
                        }
                        Debug.LogException(e);
                    }
                }

                s_Entries = null;

                foreach (var entry in entries)
                {
                    try
                    {
                        var value = valueMap[entry.id];
                        value.OnAfterMultiDeserialize(entry.json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                valueMap.Clear();
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

                serializedSet.Add(mainObject.objectId);
                serializationQueue.Add(mainObject);

                var idJsonList = new List<(string, string)>();

                // Not a foreach because the queue is populated by `JsonRef<T>`s as we go.
                for (var i = 0; i < serializationQueue.Count; i++)
                {
                    var value = serializationQueue[i];
                    var json = value.Serialize();
                    idJsonList.Add((value.objectId, json));
                }

                idJsonList.Sort((x, y) =>
                    // Main object needs to be placed first
                    x.Item1 == mainObject.objectId ? -1 :
                    y.Item1 == mainObject.objectId ? 1 :
                    // We sort everything else by ID to consistently maintain positions in the output
                    x.Item1.CompareTo(y.Item1));

                var sb = new StringBuilder();
                foreach (var (id, json) in idJsonList)
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

        public static void PopulateValueMap(JsonObject mainObject)
        {
            if (isSerializing)
            {
                throw new InvalidOperationException("Nested MultiJson serialization is not supported.");
            }

            try
            {
                isSerializing = true;

                serializedSet.Add(mainObject.objectId);
                serializationQueue.Add(mainObject);

                // Not a foreach because the queue is populated by `JsonRef<T>`s as we go.
                for (var i = 0; i < serializationQueue.Count; i++)
                {
                    var value = serializationQueue[i];
                    value.Serialize();
                    valueMap[value.objectId] = value;
                }
            }
            finally
            {
                serializationQueue.Clear();
                serializedSet.Clear();
                isSerializing = false;
            }
        }

        public static bool TryGetType(string typeName, out Type type)
        {
            type = null;
            return k_TypeMap.TryGetValue(typeName, out type);
            
        }
    }
}
