using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.SerializationDemo
{
    class ClassA : IPersistent
    {
        public string value { get; set; }
        public ClassB classB { get; set; }
    }

    class ClassB : IPersistent
    {
        public string value { get; set; }
        public ClassC classC;
    }

    class ClassC
    {
        public string value { get; set; }
    }

    [InitializeOnLoad]
    static class JsonTest
    {
        static JsonTest()
        {
            var classB = new ClassB { value = "B", classC = new ClassC { value = "C" } };
            var classA = new ClassA { value = "A", classB = classB };
            var set = new PersistentSet { classA, classB };
            var json = set.ToJson();
            var deserializedSet = PersistentSet.FromJson(json);

            Debug.Log(json);
            Debug.Log(deserializedSet.ToJson());
            Debug.Log($"ClassA.classB == ClassB: {ReferenceEquals(deserializedSet.First<ClassA>().classB, deserializedSet.First<ClassB>())}");

            var legacyJson = File.ReadAllText("Assets/TestbedAssets/MiscSGs/PartyPreview.ShaderGraph");
            LoadLegacyGraph(legacyJson);
        }

        class LegacyNestedDeserializer<T> where T : IPersistent
        {
            readonly JObject m_LegacyJObject;
            readonly PersistentSet m_Set;
            readonly JsonSerializer m_Serializer;
            readonly Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> m_TypeRemapping;
            readonly string m_CollectionPropertyName;
            readonly Func<JObject, string> m_GetGetGuidPropertyName;
            readonly Dictionary<string, T> m_LegacyMap = new Dictionary<string, T>();
            readonly List<(T, JObject)> m_ProcessingQueue = new List<(T, JObject)>();

            public LegacyNestedDeserializer(PersistentSet set,
                JsonSerializer serializer,
                Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> typeRemapping,
                JObject legacyJObject,
                string collectionPropertyName,
                Func<JObject, string> getGuidPropertyName)
            {
                m_LegacyJObject = legacyJObject;
                m_Set = set;
                m_Serializer = serializer;
                m_TypeRemapping = typeRemapping;
                m_CollectionPropertyName = collectionPropertyName;
                m_GetGetGuidPropertyName = getGuidPropertyName;

                CreateInstances();
            }

            public List<(T, JObject)> processingQueue
            {
                get => m_ProcessingQueue;
            }

            void CreateInstances()
            {
                var jTokens = m_LegacyJObject.Value<JArray>(m_CollectionPropertyName);
                foreach (var jToken in jTokens)
                {
                    var serializedElement = m_Serializer.Deserialize<SerializationHelper.JSONSerializedElement>(jToken.CreateReader());
                    var typeInfo = SerializationHelper.DoTypeRemap(serializedElement.typeInfo, m_TypeRemapping);
                    Type type = null;
                    foreach (var candidateType in TypeCache.GetTypesDerivedFrom<T>())
                    {
                        if (candidateType.FullName == typeInfo.fullName)
                        {
                            type = candidateType;
                            break;
                        }
                    }

                    if (type == null || type.GetConstructor(new Type[0]) == null)
                    {
                        // TODO: Put an error somewhere
                        continue;
                    }

                    var jObject = JObject.Parse(serializedElement.JSONnodeData);
                    var nodeGuid = m_GetGetGuidPropertyName(jObject);
                    var instance = (T)Activator.CreateInstance(type);
                    m_LegacyMap[nodeGuid] = instance;
                    m_Set.Add(instance);
                    m_ProcessingQueue.Add((instance, jObject));
                }
            }
        }

        static List<(T, JObject)> ParseAndCreateElements<T>(JArray jArray, JsonSerializer serializer, Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> typeRemapping)
        {
            var result = new List<(T, JObject)>();
            if (jArray == null)
            {
                return result;
            }
            foreach (var jToken in jArray)
            {
                var serializedElement = serializer.Deserialize<SerializationHelper.JSONSerializedElement>(jToken.CreateReader());
                var typeInfo = SerializationHelper.DoTypeRemap(serializedElement.typeInfo, typeRemapping);
                Type type = null;
                foreach (var candidateType in TypeCache.GetTypesDerivedFrom<T>())
                {
                    if (candidateType.FullName == typeInfo.fullName)
                    {
                        type = candidateType;
                        break;
                    }
                }

                if (type == null || type.GetConstructor(new Type[0]) == null)
                {
                    // TODO: Put an error somewhere
                    continue;
                }

                var jObject = JObject.Parse(serializedElement.JSONnodeData);
                var instance = (T)Activator.CreateInstance(type);
                result.Add((instance, jObject));
            }

            return result;
        }

        struct LegacyData<T>
        {
            public List<(T, JObject)> queue;
            public Dictionary<string, T> map;
        }

        static void LoadLegacyGraph(string json)
        {
            var set = new PersistentSet();
            var jObject = JObject.Parse(json);

            // m_Groups
            // m_SerializableEdges (consider whether these should be persistent)
            // m_StickyNotes
//            var groupJTokens = legacyJObject.Value<JArray>("m_Groups");
//            var edgeJTokens = legacyJObject.Value<JArray>("m_SerializableEdges");

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new ShaderGraphContractResolver(),
                Converters = new List<JsonConverter>() { new Vector4Converter() }
            });
            var typeRemapping = GraphUtil.GetLegacyTypeRemapping();

            const string legacyNodesKey = "m_SerializableNodes";
            var nodeLegacyData = new LegacyData<AbstractMaterialNode>();
            if (jObject.ContainsKey(legacyNodesKey))
            {
                nodeLegacyData.queue = ParseAndCreateElements<AbstractMaterialNode>(jObject.Value<JArray>(legacyNodesKey), serializer, typeRemapping);
                nodeLegacyData.map = nodeLegacyData.queue.ToDictionary(x => x.Item2["m_GuidSerialized"].Value<string>(), x => x.Item1);
                set.AddRange(nodeLegacyData.queue.Select(x => x.Item1));
                jObject.Remove(legacyNodesKey);
            }

            foreach (var (node, jNode) in nodeLegacyData.queue)
            {
                var slotQueue = ParseAndCreateElements<MaterialSlot>(jNode.Value<JArray>("m_SerializableSlots"), serializer, typeRemapping);
                foreach (var (materialSlot, jSlot) in slotQueue)
                {
                    Debug.Log(jSlot);
                    serializer.Populate(jSlot.CreateReader(), materialSlot);
                    var stringWriter = new StringWriter();
                    serializer.Serialize(stringWriter, materialSlot);
                    Debug.Log(stringWriter.ToString());
                }
            }

            const string legacyEdgesKey = "m_SerializableEdges";
            if (jObject.ContainsKey(legacyEdgesKey))
            {
//                var edgeQueue = ParseAndCreateElements<Edge
            }

            const string legacyPropertiesKey = "m_SerializedProperties";
            var propertyLegacyData = new LegacyData<AbstractShaderProperty>();
            if (jObject.ContainsKey(legacyPropertiesKey))
            {
                propertyLegacyData.queue = ParseAndCreateElements<AbstractShaderProperty>(jObject.Value<JArray>(legacyPropertiesKey), serializer, typeRemapping);
                propertyLegacyData.map = propertyLegacyData.queue.ToDictionary(x => x.Item2["m_Guid"]["m_GuidSerialized"].Value<string>(), x => x.Item1);
                set.AddRange(propertyLegacyData.queue.Select(x => x.Item1));
            }

            const string legacyKeywordsKey = "m_SerializedKeywords";
            var keywordLegacyData = new LegacyData<ShaderKeyword>();
            if (jObject.ContainsKey(legacyKeywordsKey))
            {
                keywordLegacyData.queue = ParseAndCreateElements<ShaderKeyword>(jObject.Value<JArray>(legacyKeywordsKey), serializer, typeRemapping);
                keywordLegacyData.map = keywordLegacyData.queue.ToDictionary(x => x.Item2["m_Guid"]["m_GuidSerialized"].Value<string>(), x => x.Item1);
                set.AddRange(keywordLegacyData.queue.Select(x => x.Item1));
            }

//            var propertyDeserializer = new LegacyNestedDeserializer<AbstractShaderProperty>(set, serializer, typeRemapping, legacyJObject,
//                "m_SerializedProperties",
//                x => x["m_Guid"]["m_GuidSerialized"].Value<string>());
//            var keywordDeserializer = new LegacyNestedDeserializer<ShaderKeyword>(set, serializer, typeRemapping, legacyJObject,
//                "m_SerializedKeywords",
//                x => x["m_Guid"]["m_GuidSerialized"].Value<string>());
        }
    }
}
