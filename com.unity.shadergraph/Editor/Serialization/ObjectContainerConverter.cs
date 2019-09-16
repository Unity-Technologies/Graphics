using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEditor.ShaderGraph.Serialization
{
//    class ObjectContainerConverter : JsonConverter<PersistentSet>
//    {
//        public override bool CanWrite => false;
//
//        public override void WriteJson(JsonWriter writer, PersistentSet value, JsonSerializer serializer)
//        {
//            var referenceResolver = new ReferenceResolver { referenceMap = value };
//            serializer.ReferenceResolver = referenceResolver;
//            writer.WriteStartObject();
//            foreach (var obj in value)
//            {
//                var id = value.GetId(obj);
//                writer.WritePropertyName(id);
//                referenceResolver.nextIsSource = true;
//                serializer.Serialize(writer, obj, typeof(IPersistent));
//            }
//            writer.WriteEndObject();
//        }
//
//        public override PersistentSet ReadJson(JsonReader reader, Type objectType, PersistentSet persistentSet, bool hasExistingValue, JsonSerializer serializer)
//        {
//            persistentSet = hasExistingValue ? persistentSet : new PersistentSet();
//            var objectMap = new Dictionary<string, IPersistent>();
//
//            var jObjects = new List<(JObject, IPersistent)>();
//            var jArray = JArray.Load(reader);
//            foreach (var jToken in jArray)
//            {
//                if (jToken is JObject jObject && jObject.ContainsKey("$type") && jObject.ContainsKey("$id"))
//                {
//                    var (assemblyName, typeName) = SplitFullyQualifiedTypeName(jObject["$type"].Value<string>());
//                    var type = serializer.SerializationBinder.BindToType(assemblyName, typeName);
//                    var id = jObject["$id"].Value<string>();
//                    if (typeof(IPersistent).IsAssignableFrom(type))
//                    {
//                        var obj = (IPersistent)Activator.CreateInstance(type);
//                        objectMap[id] = obj;
//                        jObjects.Add((jObject, obj));
//                    }
//                }
//            }
//
//            var resolver = new ReferenceResolver { objectMap = objectMap, referenceMap = persistentSet };
//            serializer.ReferenceResolver = resolver;
//
//            foreach (var (jObject, existingObject) in jObjects)
//            {
//                resolver.nextIsSource = true;
//                serializer.Populate(jObject.CreateReader(), existingObject);
//            }
//
//            foreach (var obj in objectMap.Values)
//            {
//                persistentSet.Add(obj);
//            }
//
//            return persistentSet;
//        }
//
//        static (string, string) SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
//        {
//            int? assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);
//
//            string typeName;
//            string assemblyName;
//
//            if (assemblyDelimiterIndex != null)
//            {
//                typeName = fullyQualifiedTypeName.Substring(0, assemblyDelimiterIndex.GetValueOrDefault()).Trim();
//                assemblyName = fullyQualifiedTypeName.Substring(assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1).Trim();
//            }
//            else
//            {
//                typeName = fullyQualifiedTypeName;
//                assemblyName = null;
//            }
//
//            return (assemblyName, typeName);
//        }
//
//        static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
//        {
//            // we need to get the first comma following all surrounded in brackets because of generic types
//            // e.g. System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
//            int scope = 0;
//            for (int i = 0; i < fullyQualifiedTypeName.Length; i++)
//            {
//                char current = fullyQualifiedTypeName[i];
//                switch (current)
//                {
//                    case '[':
//                        scope++;
//                        break;
//                    case ']':
//                        scope--;
//                        break;
//                    case ',':
//                        if (scope == 0)
//                        {
//                            return i;
//                        }
//
//                        break;
//                }
//            }
//
//            return null;
//        }
//    }
}
