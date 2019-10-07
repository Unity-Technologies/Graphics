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
    class ClassA : IJsonObject
    {
        public string value { get; set; }
        public ClassB classB { get; set; }
    }

    class ClassB : IJsonObject
    {
        public string value { get; set; }
        public ClassC classC;
    }

    class ClassC
    {
        public string value { get; set; }
    }

//    [InitializeOnLoad]
    static class JsonTest
    {
        static JsonTest()
        {
            var classB = new ClassB { value = "B", classC = new ClassC { value = "C" } };
            var classA = new ClassA { value = "A", classB = classB };
            var set = new JsonStore { classA, classB };
            var json = set.Serialize(classA);
            var deserializedSet = JsonStore.Deserialize(json);

            Debug.Log(json);
            Debug.Log(deserializedSet.Serialize(classA));
            Debug.Log($"ClassA.classB == ClassB: {ReferenceEquals(deserializedSet.First<ClassA>().classB, deserializedSet.First<ClassB>())}");

            var legacyJson = File.ReadAllText("Assets/Testing/IntegrationTests/Graphs/Math/Interpolation/Lerp.ShaderGraph");
            LoadLegacyGraph(legacyJson);
        }

        class LegacyNestedDeserializer<T> where T : IJsonObject
        {
            readonly JObject m_LegacyJObject;
            readonly JsonStore m_Set;
            readonly JsonSerializer m_Serializer;
            readonly Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> m_TypeRemapping;
            readonly string m_CollectionPropertyName;
            readonly Func<JObject, string> m_GetGetGuidPropertyName;
            readonly Dictionary<string, T> m_LegacyMap = new Dictionary<string, T>();
            readonly List<(T, JObject)> m_ProcessingQueue = new List<(T, JObject)>();

            public LegacyNestedDeserializer(JsonStore set,
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
                    throw new InvalidOperationException("Missing default constructor");
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
            var set = new JsonStore {};
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new ContractResolver(),
                Converters = new List<JsonConverter>() { new Vector2Converter(), new Vector3Converter(), new Vector4Converter(), new ColorConverter(), new Matrix4x4Converter() },
                Formatting = Formatting.Indented,
                ReferenceResolverProvider = () => new ReferenceResolver { jsonStore = set }
            });
            var graphData = serializer.Deserialize<GraphData>(new JsonTextReader(new StringReader(json)));
            set.Add(graphData);
            set.Serialize(graphData, Formatting.None);
            Debug.Log(set.Serialize(graphData));
        }

        static void LoadLegacyGraph2(string json)
        {
            var set = new JsonStore();
            var jObject = JObject.Parse(json);

            // m_Groups
            // m_SerializableEdges (consider whether these should be persistent)
            // m_StickyNotes
//            var groupJTokens = legacyJObject.Value<JArray>("m_Groups");
//            var edgeJTokens = legacyJObject.Value<JArray>("m_SerializableEdges");

            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new ContractResolver(),
                Converters = new List<JsonConverter>() { new Vector2Converter(), new Vector3Converter(), new Vector4Converter(), new ColorConverter(), new Matrix4x4Converter() },
                Formatting = Formatting.Indented
            });
            var typeRemapping = GraphUtil.GetLegacyTypeRemapping();

            var processingQueue = new List<(object, JObject)>();

            const string legacyNodesKey = "m_SerializableNodes";
            var legacyMap = new Dictionary<string, IJsonObject>();
            var nodeSlots = new List<(AbstractMaterialNode, List<MaterialSlot>)>();
            if (jObject.ContainsKey(legacyNodesKey))
            {
                var queue = ParseAndCreateElements<AbstractMaterialNode>(jObject.Value<JArray>(legacyNodesKey), serializer, typeRemapping);
                foreach (var (node, nodeJObject) in queue)
                {
                    set.Add(node);
                    processingQueue.Add((node, nodeJObject));
                    var legacyGuid = nodeJObject.Value<string>("m_GuidSerialized");
                    legacyMap.Add($"node:{legacyGuid}", node);
                    const string legacySlotsKey = "m_SerializableSlots";
                    var slotQueue = ParseAndCreateElements<MaterialSlot>(nodeJObject.Value<JArray>(legacySlotsKey), serializer, typeRemapping);
                    var slotList = new List<MaterialSlot>();
                    foreach (var (slot, slotJObject) in slotQueue)
                    {
                        set.Add(slot);
                        processingQueue.Add((slot, slotJObject));
                        var slotId = slotJObject.Value<int>("m_Id");
                        legacyMap.Add($"slot:{legacyGuid}.{slotId}", slot);
                        slotList.Add(slot);
                    }
                    nodeSlots.Add((node, slotList));
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

            var referenceResolver = new ReferenceResolver { };
            serializer.ReferenceResolver = referenceResolver;

            var writer = new StringWriter();
            foreach (var (obj, objJObject) in processingQueue)
            {
                serializer.Populate(objJObject.CreateReader(), obj);
            }

            foreach (var (node, slots) in nodeSlots)
            {
                foreach (var slot in slots)
                {
                    node.AddSlot(slot);
                }
            }

            foreach (var (obj, _) in processingQueue)
            {
                writer.WriteLine(obj.GetType().FullName);
                referenceResolver.nextIsSource = true;
                serializer.Serialize(writer, obj, typeof(IJsonObject));
                writer.WriteLine();
                writer.WriteLine();
            }

            Debug.Log(writer.ToString());
//            var propertyDeserializer = new LegacyNestedDeserializer<AbstractShaderProperty>(set, serializer, typeRemapping, legacyJObject,
//                "m_SerializedProperties",
//                x => x["m_Guid"]["m_GuidSerialized"].Value<string>());
//            var keywordDeserializer = new LegacyNestedDeserializer<ShaderKeyword>(set, serializer, typeRemapping, legacyJObject,
//                "m_SerializedKeywords",
//                x => x["m_Guid"]["m_GuidSerialized"].Value<string>());
        }
    }
}
