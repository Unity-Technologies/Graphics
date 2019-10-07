using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.ShaderGraph.SerializationDemo;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class JsonStore : ICollection<IJsonObject>
    {
        public Dictionary<IJsonObject, string> objectMap = new Dictionary<IJsonObject, string>();
        public Dictionary<string, IJsonObject> referenceMap = new Dictionary<string, IJsonObject>();

        public T First<T>() where T : IJsonObject
        {
            foreach (var obj in objectMap.Keys)
            {
                if (obj is T value)
                {
                    return value;
                }
            }

            throw new InvalidOperationException($"Collection does not contain an object of type {typeof(T)}.");
        }

        public IJsonObject Get(string reference)
        {
            return referenceMap[reference];
        }

        public string GetId(IJsonObject jsonObject)
        {
            return objectMap[jsonObject];
        }

        public string GetOrAddId(IJsonObject jsonObject)
        {
            if (!objectMap.ContainsKey(jsonObject))
            {
                Add(jsonObject);
            }

            return objectMap[jsonObject];
        }

        static List<JsonConverter> s_Converters => new List<JsonConverter>
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new ColorConverter(),
            new RectConverter(),
            new Matrix4x4Converter(),
            new UnityObjectConverter()
        };

        public string Serialize(IJsonObject root, Formatting formatting = Formatting.Indented)
        {
            var queue = new Queue<IJsonObject>();
            queue.Enqueue(root);
            var referenceResolver = new ReferenceResolver { jsonStore = this, queue = queue, visited = new HashSet<IJsonObject>()};
            referenceResolver.visited.Add(root);
            var settings = new JsonSerializerSettings
            {
                Formatting = formatting,
                Converters = s_Converters,
                ReferenceResolverProvider = () => referenceResolver,
                ContractResolver = new ContractResolver(),
                TypeNameHandling = TypeNameHandling.Auto
            };
            var properties = new List<(string, string)>();
            var serializer = JsonSerializer.Create(settings);
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            while (queue.Count > 0)
            {
                sb.Clear();
                var jsonWriter = new JsonTextWriter(stringWriter);
                jsonWriter.Formatting = formatting;
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("");
                var startIndex = sb.Length;
                var item = queue.Dequeue();
                referenceResolver.nextIsSource = true;
                serializer.Serialize(jsonWriter, item, typeof(IJsonObject));
                var json = sb.ToString(startIndex, sb.Length - startIndex);
                properties.Add((GetId(item), json.TrimStart()));
            }

            properties.Sort((x1, x2) => x1.Item1.CompareTo(x2.Item2));
            sb.Clear();
            var writer = new JsonTextWriter(stringWriter);
            writer.Formatting = formatting;
            writer.WriteStartArray();
            foreach (var (_, json) in properties)
            {
                writer.WriteRawValue(json);
            }

            writer.WriteEndArray();
            return sb.ToString();
        }

        public static JsonStore Deserialize(string json)
        {
            JArray root;

            // Handle legacy graphs
            if (json.StartsWith("{"))
            {
                var graphDataJson = JObject.Parse(json);
                graphDataJson["$id"] = Guid.NewGuid().ToString();
                graphDataJson["$type"] = typeof(GraphData).AssemblyQualifiedName;
                root = new JArray
                {
                    graphDataJson
                };
            }
            else
            {
                root = JArray.Parse(json);
            }

            var persistentSet = new JsonStore();
            var jObjects = new List<DeserializationPair>();
            var resolver = new ReferenceResolver
            {
                jsonStore = persistentSet,
                jObjects = jObjects
            };
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new ContractResolver(),
                Converters = s_Converters,
                ReferenceResolverProvider = () => resolver
            });

            foreach (var jToken in root)
            {
                if (!(jToken is JObject jObject) || !jObject.ContainsKey("$type"))
                {
                    continue;
                }

                var (assemblyName, typeName) = SplitFullyQualifiedTypeName(jObject["$type"].Value<string>());
                var type = serializer.SerializationBinder.BindToType(assemblyName, typeName);
                var id = jObject.Value<string>("$id");
                if (typeof(IJsonObject).IsAssignableFrom(type))
                {
                    var obj = (IJsonObject)Activator.CreateInstance(type);
                    persistentSet.Add(id, obj);
                    jObjects.Add(new DeserializationPair(obj, jObject));
                }
            }

            for (var i = 0; i < jObjects.Count; i++)
            {
                var (instance, jObject) = jObjects[i];
                resolver.nextIsSource = true;
                serializer.Populate(jObject.CreateReader(), instance);
            }

            foreach (var (instance, jObject) in jObjects)
            {
                if (instance is IOnDeserialized callback)
                {
                    callback.OnDeserialized(jObject, jObjects);
                }
            }

            //            foreach (var kvp in objectMap)
            //            {
            //                persistentSet.Add(kvp.Key, kvp.Value);
            //            }

            return persistentSet;
        }

        static (string, string) SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
        {
            int? assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);

            string typeName;
            string assemblyName;

            if (assemblyDelimiterIndex != null)
            {
                typeName = fullyQualifiedTypeName.Substring(0, assemblyDelimiterIndex.GetValueOrDefault()).Trim();
                assemblyName = fullyQualifiedTypeName.Substring(assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1).Trim();
            }
            else
            {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }

            return (assemblyName, typeName);
        }

        static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            // we need to get the first comma following all surrounded in brackets because of generic types
            // e.g. System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
            int scope = 0;
            for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                char current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        scope++;
                        break;
                    case ']':
                        scope--;
                        break;
                    case ',':
                        if (scope == 0)
                        {
                            return i;
                        }

                        break;
                }
            }

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<IJsonObject, string>.KeyCollection.Enumerator GetEnumerator() => objectMap.Keys.GetEnumerator();

        IEnumerator<IJsonObject> IEnumerable<IJsonObject>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(IJsonObject item)
        {
            var reference = Guid.NewGuid().ToString();
            objectMap.Add(item, reference);
            referenceMap.Add(reference, item);
        }

        public void Add(string reference, IJsonObject item)
        {
            objectMap.Add(item, reference);
            referenceMap.Add(reference, item);
        }

        public void AddRange<T>(T items) where T : IEnumerable<IJsonObject>
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Clear() => objectMap.Clear();

        public bool Contains(IJsonObject item) => objectMap.ContainsKey(item);

        public void CopyTo(IJsonObject[] array, int arrayIndex) => objectMap.Keys.CopyTo(array, arrayIndex);

        public bool Remove(IJsonObject item) => objectMap.Remove(item);

        public int Count => objectMap.Count;

        public bool IsReadOnly => false;
    }
}
