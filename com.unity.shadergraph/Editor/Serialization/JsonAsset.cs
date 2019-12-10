using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    static class JsonAsset
    {
        internal static Dictionary<string, Type> typeMap;

        static JsonAsset()
        {
            typeMap = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<JsonObject>())
            {
                if (type.FullName != null)
                {
                    typeMap[type.FullName] = type;
                }
            }

            var remap = GraphUtil.GetLegacyTypeRemapping();
            foreach (var pair in remap)
            {
                if (typeMap.TryGetValue(pair.Value.fullName, out var type))
                {
                    typeMap[pair.Key.fullName] = type;
                }
            }
        }

        public static List<RawJsonObject> Parse(string str)
        {
            var result = new List<RawJsonObject>();
            var separatorStr = $"{Environment.NewLine}{Environment.NewLine}";
            var startIndex = 0;
            const string headerBeginStr = "--- ";
            if (!str.StartsWith(headerBeginStr))
            {
                throw new InvalidOperationException("Expected '--- '");
            }

            while (startIndex < str.Length)
            {
                var idEnd = str.IndexOf(Environment.NewLine, startIndex, StringComparison.Ordinal);
                if (idEnd == -1)
                {
                    throw new InvalidOperationException("Expected new line");
                }

                var jsonBegin = idEnd + 1;
                var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
                if (jsonEnd == -1)
                {
                    jsonEnd = str.Length;
                }

                var typeBegin = str.IndexOf(' ', startIndex, idEnd - startIndex) + 1;
                var typeEnd = str.IndexOf(' ', typeBegin, idEnd - typeBegin);
                var idBegin = typeEnd + 1;

                if (str.IndexOf(headerBeginStr, typeBegin - 4, 4, StringComparison.Ordinal) == -1)
                {
                    throw new InvalidOperationException("Expected '--- '");
                }

                var o = new RawJsonObject
                {
                    type = str.Substring(typeBegin, typeEnd - typeBegin),
                    id = str.Substring(idBegin, idEnd - idBegin),
                    json = str.Substring(jsonBegin, jsonEnd - jsonBegin)
                };
                result.Add(o);

                startIndex = jsonEnd + separatorStr.Length;
            }

            return result;
        }

        public static int Deserialize(List<RawJsonObject> rawJsonObjects, Dictionary<string, JsonObject> objectMap, int version = 0)
        {
            using (var context = DeserializationContext.Begin(objectMap))
            {
                var queue = context.queue;
                var activeObjects = ListPool<JsonObject>.Get();
                foreach (var rawJsonObject in rawJsonObjects)
                {
                    if (objectMap.TryGetValue(rawJsonObject.id, out var jsonObject))
                    {
                        activeObjects.Add(jsonObject);
                        if (jsonObject.changeVersion != rawJsonObject.changeVersion)
                        {
                            queue.Add((jsonObject, rawJsonObject.json));
                        }
                    }
                    else
                    {
                        if (!typeMap.TryGetValue(rawJsonObject.type, out var type))
                        {
                            Debug.LogError($"Invalid type {rawJsonObject.type}");
                            continue;
                        }

                        jsonObject = (JsonObject)Activator.CreateInstance(type);
                        jsonObject.jsonId = rawJsonObject.id;
                        jsonObject.changeVersion = rawJsonObject.changeVersion;
                        objectMap[rawJsonObject.id] = jsonObject;
                        activeObjects.Add(jsonObject);
                        queue.Add((jsonObject, rawJsonObject.json));
                    }
                }

                objectMap.Clear();
                foreach (var jsonObject in activeObjects)
                {
                    objectMap.Add(jsonObject.jsonId, jsonObject);
                }

                ListPool<JsonObject>.Release(activeObjects);

                if (queue.Any())
                {
                    version++;
                }

                // Has to be a for-loop because size of queue might change during iteration
                for (var i = 0; i < queue.Count; i++)
                {
                    var (jsonObject, json) = queue[i];
                    jsonObject.changeVersion = version;
                    jsonObject.OnDeserializing();
                    EditorJsonUtility.FromJsonOverwrite(json, jsonObject);
                    jsonObject.OnDeserialized(json);
                }

                foreach (var (jsonObject, json) in queue)
                {
                    jsonObject.OnStoreDeserialized(json);
                }
            }

            return version;
        }
    }
}
