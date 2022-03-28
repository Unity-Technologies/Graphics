using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    internal struct SerializedEntry
    {
        public string key;
        public string valueType;
        public string valueEntry;
    }

    [Serializable]
    internal class MetadataBlock : Dictionary<string, ValueType>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializedEntry> serializedEntries;


        private struct DataBox<T>
        {
            public T data;
        }

        public bool HasMetadata(string lookup)
        {
            return ContainsKey(lookup);
        }

        public T GetMetadata<T>(string lookup)
        {
            return (this[lookup] as DataBox<T>?).Value.data;
        }

        public void SetMetadata<T>(string lookup, T data)
        {
            this[lookup] = new DataBox<T> { data = data };
        }

        public void OnBeforeSerialize()
        {
            serializedEntries = new List<SerializedEntry>();
            foreach(var (key,value) in this)
            {
                serializedEntries.Add(new SerializedEntry()
                {
                    key = key,
                    valueType = value.GetType().GetGenericArguments()[0].AssemblyQualifiedName,
                    valueEntry = EditorJsonUtility.ToJson(value)
                });
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(var entry in serializedEntries)
            {
                Type generic = typeof(DataBox<>);
                Type valueType;
                try
                {
                    valueType = Type.GetType(entry.valueType);
                }
                catch
                {
                    Debug.LogError($"Could not deserialize the data on metadata {entry.key} of type {entry.valueType}; skipping");
                    continue;
                }
                var constructedType = generic.MakeGenericType(valueType);
                var dataBox = JsonUtility.FromJson(entry.valueEntry, constructedType);
                var dataField = constructedType.GetField("data");
                var data = dataField.GetValue(dataBox);
                typeof(MetadataBlock).GetMethod("SetMetadata").MakeGenericMethod(valueType).Invoke(this, new object[] { entry.key, data });
            }
        }
    }

    [Serializable]
    internal struct SerializedBlock
    {
        public string key;
        public MetadataBlock block;
    }

    [Serializable]
    internal class MetadataCollection : Dictionary<string, MetadataBlock>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializedBlock> serializedBlocks;
        public void OnAfterDeserialize()
        {
            foreach(var block in serializedBlocks)
            {
                Add(block.key, block.block);
            }
            serializedBlocks = null;
        }

        public void OnBeforeSerialize()
        {
            serializedBlocks = new List<SerializedBlock>();
            foreach(var (key, value) in this)
            {
                serializedBlocks.Add(new SerializedBlock()
                {
                    key = key,
                    block = value
                });
            }
        }
    }
}
