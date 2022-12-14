using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    internal struct LayerDescriptor
    {
        public string layerName;
        public bool isSerialized;

        public LayerDescriptor(string layerName, bool isSerialized = false)
        {
            this.layerName = layerName;
            this.isSerialized = isSerialized;
        }
    }

    [Serializable]
    internal class LayerList : ICollection<KeyValuePair<int, (LayerDescriptor descriptor, Element element)>>,  ISerializationCallbackReceiver
    {
        //[SerializeReference]
        private ContextLayeredDataStorage owner;

        [SerializeField]
        List<int> m_SerializedKeys;

        [SerializeField]
        List<LayerDescriptor> m_SerializedLayerDescriptors;

        [SerializeReference]
        List<Element> m_SerializedElements;

        SortedList<int, (LayerDescriptor descriptor, Element element)> m_SortedList;

        private class ReverseIntComparer : IComparer<int>
        {
            //reverse order since all our searching will check highest layer first
            public int Compare(int x, int y) => x.CompareTo(y) * -1;
        }

        public LayerList(ContextLayeredDataStorage owner)
        {
            m_SortedList = new SortedList<int, (LayerDescriptor descriptor, Element element)>(new ReverseIntComparer());
            this.owner = owner;
        }

        public void AddLayer(int priority, string name, bool isSerialized = false)
        {
            m_SortedList.Add(priority, (new LayerDescriptor(name, isSerialized), new IntElement(ElementID.FromString(""), priority, owner)));
        }

        public void AddNewTopLayer(string name)
        {
            AddLayer(m_SortedList.Keys[0] + 1, name);
        }

        public Element GetLayerRoot(string name)
        {
            foreach (var (_, tup) in this)
            {
                if (string.CompareOrdinal(name, tup.descriptor.layerName) == 0)
                {
                    return tup.element;
                }
            }
            return null;
        }

        public Element GetTopLayerRoot()
        {
            if (m_SortedList.Values != null && m_SortedList.Values.Count > 0)
            {
                return m_SortedList.Values[0].element;
            }
            return null;
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            m_SerializedKeys = m_SortedList.Keys.ToList();
            m_SerializedLayerDescriptors = m_SortedList.Values.Select(v => v.descriptor).ToList();
            m_SerializedElements = m_SortedList.Values.Select(v => v.element).ToList();
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            Clear();
            for (var i = 0; i < m_SerializedKeys.Count; i++)
            {
                m_SortedList.Add(m_SerializedKeys[i], (m_SerializedLayerDescriptors[i], m_SerializedElements[i]));
            }
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<int, (LayerDescriptor descriptor, Element element)>> GetEnumerator()
        {
            return m_SortedList.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_SortedList).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<int, (LayerDescriptor descriptor, Element element)> item)
        {
            m_SortedList.Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            m_SortedList.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<int, (LayerDescriptor descriptor, Element element)> item)
        {
            return m_SortedList.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<int, (LayerDescriptor descriptor, Element element)>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<int, (LayerDescriptor descriptor, Element element)> item)
        {
            return m_SortedList.Remove(item.Key);
        }

        /// <inheritdoc />
        public int Count => m_SortedList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => throw new NotImplementedException();
    }

    //Stores a layers data
    [Serializable]
    internal class SerializedLayerData
    {
        public string layerName;
        public List<SerializedElementData> layerData;

        public SerializedLayerData()
        {
            this.layerData = null;
            this.layerData = null;
        }
        public SerializedLayerData(string layerName, List<SerializedElementData> layerData)
        {
            this.layerName = layerName;
            this.layerData = layerData;
        }
    }

}
