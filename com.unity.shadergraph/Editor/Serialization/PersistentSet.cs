using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.ShaderGraph.SerializationDemo;

namespace UnityEditor.ShaderGraph.Serialization
{
    class PersistentSet : ICollection<IPersistent>
    {
        Dictionary<IPersistent, string> m_Objects = new Dictionary<IPersistent, string>();

        public T First<T>() where T : IPersistent
        {
            foreach (var obj in m_Objects.Keys)
            {
                if (obj is T value)
                {
                    return value;
                }
            }

            throw new InvalidOperationException($"Collection does not contain an object of type {typeof(T)}.");
        }

        public string GetId(IPersistent persistent)
        {
            return m_Objects[persistent];
        }

        public string ToJson(Formatting formatting = Formatting.Indented)
        {
            var persistentMap = new InternalPersistentMap();
            foreach (var kvp in m_Objects)
            {
                persistentMap.Add(kvp.Value, kvp.Key);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = formatting,
                ReferenceResolverProvider = () => new ReferenceResolver { referenceMap = m_Objects }
            };
            return JsonConvert.SerializeObject(persistentMap, settings);
        }

        public static PersistentSet FromJson(string json)
        {
            var persistentSet = new PersistentSet();
            var objectMap = new Dictionary<string, IPersistent>();
            var jObjects = new List<(JObject, IPersistent)>();
            var root = JObject.Parse(json);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new ShaderGraphContractResolver()
                {
                    NamingStrategy = new LowerCamelCaseNamingStrategy()
                }
            });

            foreach (var jProperty in root.Properties())
            {
                if (!(jProperty.Value is JObject jObject) || !jObject.ContainsKey("$type"))
                {
                    continue;
                }

                var (assemblyName, typeName) = SplitFullyQualifiedTypeName(jObject["$type"].Value<string>());
                var type = serializer.SerializationBinder.BindToType(assemblyName, typeName);
                var id = jProperty.Name;
                if (typeof(IPersistent).IsAssignableFrom(type))
                {
                    var obj = (IPersistent)Activator.CreateInstance(type);
                    objectMap[id] = obj;
                    jObjects.Add((jObject, obj));
                }
            }

            var resolver = new ReferenceResolver { objectMap = objectMap, referenceMap = persistentSet.m_Objects };
            serializer.ReferenceResolver = resolver;

            foreach (var (jObject, existingObject) in jObjects)
            {
                resolver.nextIsSource = true;
                serializer.Populate(jObject.CreateReader(), existingObject);
            }

            foreach (var kvp in objectMap)
            {
                persistentSet.m_Objects.Add(kvp.Value, kvp.Key);
            }

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
        public Dictionary<IPersistent, string>.KeyCollection.Enumerator GetEnumerator() => m_Objects.Keys.GetEnumerator();

        IEnumerator<IPersistent> IEnumerable<IPersistent>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(IPersistent item)
        {
            m_Objects.Add(item, Guid.NewGuid().ToString());
        }

        public void AddRange<T>(T items) where T : IEnumerable<IPersistent>
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Clear() => m_Objects.Clear();

        public bool Contains(IPersistent item) => m_Objects.ContainsKey(item);

        public void CopyTo(IPersistent[] array, int arrayIndex) => m_Objects.Keys.CopyTo(array, arrayIndex);

        public bool Remove(IPersistent item) => m_Objects.Remove(item);

        public int Count => m_Objects.Count;

        public bool IsReadOnly => false;
    }
}
