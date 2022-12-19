using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    struct DataBox<T>
    {
        public T data;
    }

    [Serializable]
    internal struct SerializedEntry
    {
        public string key;
        [SerializeReference]
        public object value;
    }

    [Serializable]
    internal class MetadataBlock : Dictionary<string, object>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializedEntry> serializedEntries;

        public bool HasMetadata(string lookup)
        {
            return ContainsKey(lookup);
        }

        public T GetMetadata<T>(string lookup)
        {
            return ((DataBox<T>)this[lookup]).data;
        }

        public void SetMetadata<T>(string lookup, T data)
        {
            this[lookup] = new DataBox<T>() { data = data };
        }

        public void OnBeforeSerialize()
        {
            serializedEntries = new List<SerializedEntry>();
            foreach(var (key,value) in this)
            {
                serializedEntries.Add(new SerializedEntry()
                {
                    key = key,
                    value = value
                });
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(var entry in serializedEntries)
            {
                this[entry.key] = entry.value;
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
    internal class MetadataCollection : Dictionary<ElementID, MetadataBlock>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializedBlock> serializedBlocks;

        private ContextLayeredDataStorage owner;

        public MetadataCollection(ContextLayeredDataStorage owner) : base(new ElementIDComparer())
        {
            this.owner = owner;
        }
        public void OnAfterDeserialize()
        {
            if (serializedBlocks == null)
                return;

            foreach(var block in serializedBlocks)
            {
                Add(block.key, block.block);
            }
            serializedBlocks = null;
        }

        public void OnBeforeSerialize()
        {
            serializedBlocks = new List<SerializedBlock>();
            foreach (var (key, value) in this)
            {
                if (owner.GetHierarchyValue(owner.Search(key).Element) > 0)
                {
                    serializedBlocks.Add(new SerializedBlock()
                    {
                        key = key.FullPath,
                        block = value
                    });
                }

            }
        }
    }
}
