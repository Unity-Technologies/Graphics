using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct SerializedJsonObject
    {
        public string type;
        public string id;
        public int version;
        public string json;
    }

    class JsonObjectMetadata
    {
        public string id;
        public int version;
    }

    class JsonStore : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<SerializedJsonObject> m_SerializedObjects = new List<SerializedJsonObject>();

        [SerializeField]
        int m_SerializedVersion = 0;

        bool m_MightHaveChanges;
        bool m_ShouldReheat;

        [SerializeField]
        string m_RootId;

        [field: NonSerialized]
        public int version { get; private set; }

        public Dictionary<string, JsonObject> objectMap { get; } = new Dictionary<string, JsonObject>();

        public JsonObject root
        {
            get => Get(m_RootId);
            set
            {
                m_RootId = value.jsonId;
                objectMap[m_RootId] = value;
            }
        }

        public T First<T>() where T : JsonObject
        {
            foreach (var reference in objectMap.Values)
            {
                if (reference is T value)
                {
                    return value;
                }
            }

            throw new InvalidOperationException($"Collection does not contain an object of type {typeof(T)}.");
        }

        public JsonObject Get(string id)
        {
            if (objectMap.TryGetValue(id, out var reference))
            {
                return reference;
            }

            return null;
        }

        List<(SerializedJsonObject, JsonObject)> SerializeObjects(JsonObject root, bool prettyPrint)
        {
            if (root == null)
            {
                throw new InvalidOperationException("Cannot serialize a null root JsonObject.");
            }

            var serializeObjects = new List<(SerializedJsonObject, JsonObject)>();

            using (var context = SerializationContext.Begin(this))
            {
                objectMap.Clear();
                var queue = context.queue;
                queue.Enqueue(root);
                context.visited.Add(root);
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    if (item == null)
                    {
                        continue;
                    }

                    objectMap.Add(item.jsonId, item);
                    serializeObjects.Add((new SerializedJsonObject
                    {
                        type = item.GetType().FullName,
                        id = item.jsonId,
                        json = EditorJsonUtility.ToJson(item, prettyPrint),
                    }, item));
                }
            }

            serializeObjects.Sort((x1, x2) => x1.Item1.id.CompareTo(x2.Item1.id));
            return serializeObjects;
        }

        public string Serialize(bool prettyPrint)
        {
            var serializedObjects = SerializeObjects(root, prettyPrint);
            var sb = new StringBuilder();
            foreach (var (serializedObject, _) in serializedObjects)
            {
                sb.AppendLine($"--- {serializedObject.type} {serializedObject.id}");
                sb.AppendLine(serializedObject.json);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void CollectObjects()
        {
            SerializeObjects(root, false);
        }

        internal static Dictionary<string, Type> typeMap = CreateTypeMap();

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

            var remap = GraphUtil.GetLegacyTypeRemapping();
            foreach (var pair in remap)
            {
                if (map.TryGetValue(pair.Value.fullName, out var type))
                {
                    map[pair.Key.fullName] = type;
                }
            }

            return map;
        }

        public static JsonStore Deserialize(string str)
        {
            var rawItems = new List<RawJsonObject>();

            // Handle legacy graphs
            if (str.StartsWith("{"))
            {
                rawItems.Add(new RawJsonObject
                {
                    typeFullName = typeof(GraphData).FullName,
                    id = Guid.NewGuid().ToString(),
                    json = str
                });
            }
            else
            {
                rawItems = JsonAsset.Parse(str);
            }

            var jsonStore = CreateInstance<JsonStore>();
            jsonStore.hideFlags = HideFlags.HideAndDontSave;

            using (var context = DeserializationContext.Begin(jsonStore))
            {
                var items = context.queue;
                foreach (var rawItem in rawItems)
                {
                    if (!typeMap.TryGetValue(rawItem.typeFullName, out var type))
                    {
                        Debug.LogWarning($"Could not find type {rawItem.typeFullName}");
                        // TODO: Handle fallback
                        continue;
                    }

                    var obj = (JsonObject)Activator.CreateInstance(type);
                    obj.jsonId = rawItem.id;
                    Assert.AreEqual(rawItem.id, obj.jsonId);
                    jsonStore.objectMap.Add(obj.jsonId, obj);
                    items.Add((obj, rawItem.json));
                }

                for (var i = 0; i < items.Count; i++)
                {
                    var (instance, json) = items[i];
                    instance.OnDeserializing();
                    EditorJsonUtility.FromJsonOverwrite(json, instance);
                    instance.OnDeserialized(json);
                }

                foreach (var (instance, json) in items)
                {
                    instance.OnStoreDeserialized(json);
                }
            }

            jsonStore.m_MightHaveChanges = true;
            return jsonStore;
        }

        public void OnBeforeSerialize()
        {
//            Freeze();
        }

        public void Freeze()
        {
            if (!m_MightHaveChanges)
            {
                return;
            }

            var currentVersion = version;

            var objects = SerializeObjects(root, true);
            var serializedObjects = new List<SerializedJsonObject>();
            serializedObjects.Capacity = objects.Count;

            var serializedIndex = 0;
            var serializedCount = m_SerializedObjects.Count;
            foreach (var (serializedObject, jsonObject) in objects)
            {
                // Try to match up the object with an already serialized object, so that we can check if the JSON
                // changed, and version appropriately. This assumes that `objects` and `m_SerializedObjects` are
                // sorted by `id`.
                var index = -1;
                while (serializedIndex < serializedCount)
                {
                    var comparison = m_SerializedObjects[serializedIndex].id.CompareTo(serializedObject.id);
                    if (comparison >= 0)
                    {
                        // We only consume the current item if the id matches our id from `objects`.
                        if (comparison == 0)
                        {
                            index = serializedIndex++;
                        }

                        break;
                    }

                    serializedIndex++;
                }

                SerializedJsonObject serializedJsonObject;
                if (index != -1)
                {
                    serializedJsonObject = m_SerializedObjects[index];
                    if (!serializedJsonObject.json.Equals(serializedObject.json))
                    {
                        version = currentVersion + 1;
                        serializedJsonObject.json = serializedObject.json;
                        serializedJsonObject.version = version;
                    }
                }
                else
                {
                    version = currentVersion + 1;
                    serializedJsonObject = new SerializedJsonObject();
                    serializedJsonObject.id = serializedObject.id;
                    serializedJsonObject.json = serializedObject.json;
                    serializedJsonObject.version = version;
                    serializedJsonObject.type = jsonObject.GetType().FullName;
                }

                var instance = jsonObject;
                if (serializedJsonObject.version == version)
                {
                    instance.changeVersion = version;
                }

                serializedObjects.Add(serializedJsonObject);
            }

            m_SerializedObjects = serializedObjects;
            m_SerializedVersion = version;
            m_MightHaveChanges = false;
        }

        public void OnAfterDeserialize()
        {
            // Can't do deserialization here, as we're not allowed to use EditorJsonUtility, and we need that for
            // handling Unity Object references.
            if (m_SerializedVersion != version)
            {
                m_ShouldReheat = true;
            }
        }

        void OnEnable()
        {
//            Rehydrate();
//            Undo.undoRedoPerformed += Rehydrate;
        }

        private void OnDisable()
        {
//            Undo.undoRedoPerformed -= Rehydrate;
        }

        public void Reheat()
        {
            if (m_SerializedVersion != version)
            {
                using (var context = DeserializationContext.Begin(this))
                {
                    var items = context.queue;
                    var currentVersion = version;

                    var activeObjects = new List<JsonObject>();

                    for (var index = 0; index < m_SerializedObjects.Count; index++)
                    {
                        var serializedObject = m_SerializedObjects[index];
                        var obj = Get(serializedObject.id);
                        if (obj != null)
                        {
                            activeObjects.Add(obj);
                            if (obj.changeVersion != serializedObject.version)
                            {
                                version = currentVersion + 1;
                                serializedObject.version = version;
                                m_SerializedObjects[index] = serializedObject;
                                items.Add((obj, serializedObject.json));
                            }
                        }
                        else
                        {
                            version = currentVersion + 1;
                            serializedObject.version = version;
                            m_SerializedObjects[index] = serializedObject;
                            if (!typeMap.ContainsKey(serializedObject.type))
                            {
                                throw new InvalidOperationException($"Invalid type {serializedObject.type}");
                            }
                            var type = typeMap[serializedObject.type];
                            var instance = (JsonObject)Activator.CreateInstance(type);
                            instance.jsonId = serializedObject.id;
                            items.Add((instance, serializedObject.json));
                            activeObjects.Add(instance);
                        }
                    }

                    objectMap.Clear();
                    foreach (var activeObject in activeObjects)
                    {
                        objectMap[activeObject.jsonId] = activeObject;
                    }

                    // Must be a for-loop because the list might be modified during traversal.
                    for (var i = 0; i < items.Count; i++)
                    {
                        var (instance, json) = items[i];
                        if (!string.IsNullOrEmpty(json))
                        {
                            instance.OnDeserializing();
                            EditorJsonUtility.FromJsonOverwrite(json, instance);
                        }
                        instance.OnDeserialized(json);
                        instance.changeVersion = version;
                        objectMap[instance.jsonId] = instance;
                    }

                    foreach (var (instance, json) in items)
                    {
                        instance.OnStoreDeserialized(json);
                    }
                }

                m_SerializedVersion = version;
                m_MightHaveChanges = false;
                m_ShouldReheat = false;
            }
        }

        public void RegisterCompleteObjectUndo(string actionName)
        {
            Freeze();
            Undo.RegisterCompleteObjectUndo(this, actionName);
            m_MightHaveChanges = true;
        }

        public void CheckForChanges()
        {
            if (m_ShouldReheat)
            {
                Reheat();
            }
            else if (m_MightHaveChanges)
            {
                Freeze();
            }
        }
    }
}
