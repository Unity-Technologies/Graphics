using System;
using System.Collections.Generic;

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

    internal class LayerList : SortedList<int, (LayerDescriptor descriptor, Element element)>
    {
        private ContextLayeredDataStorage owner;
        private class ReverseIntComparer : IComparer<int>
        {
            //reverse order since all our searching will check highest layer first
            public int Compare(int x, int y) => x.CompareTo(y) * -1;
        }

        public LayerList(ContextLayeredDataStorage owner) : base(new ReverseIntComparer()) => this.owner = owner;

        public void AddLayer(int priority, string name, bool isSerialized = false)
        {
            Add(priority, (new LayerDescriptor(name, isSerialized), new Element<int>(ElementID.FromString(""), priority, owner)));
        }

        public void AddNewTopLayer(string name)
        {
            AddLayer(Keys[0] + 1, name);
        }

        public Element GetLayerRoot(string name)
        {
            foreach (var (id, elem) in Values)
            {
                if (name.CompareTo(id.layerName) == 0)
                {
                    return elem;
                }
            }
            return null;
        }

        public Element GetTopLayerRoot()
        {
            if (Values != null && Values.Count > 0)
            {
                return Values[0].element;
            }
            return null;
        }
    }

    //Stores a layers data
    [Serializable]
    internal struct SerializedLayerData
    {
        public string layerName;
        public List<SerializedElementData> layerData;

        public SerializedLayerData(string layerName, List<SerializedElementData> layerData)
        {
            this.layerName = layerName;
            this.layerData = layerData;
        }
    }

}
