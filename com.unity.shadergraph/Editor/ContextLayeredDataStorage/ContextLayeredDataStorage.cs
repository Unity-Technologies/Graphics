using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    public interface IDataElement
    {
        public bool TryGetData<T>(out T data);
    }

    [Serializable]
    public class ContextLayeredDataStorage : ISerializationCallbackReceiver
    {
        protected class Element : IDataElement
        {
            public string id;
            public List<Element> children;
            public Element parent;
            public ContextLayeredDataStorage owner;
            public string serializedData;

            public Element(ContextLayeredDataStorage owner)
            {
                id = "";
                parent = null;
                children = new List<Element>();
                this.owner = owner;
            }

            public Element(string id, ContextLayeredDataStorage owner) : this(owner)
            {
                this.id = id;
            }

            //It is a path id if there are any '.' characters
            public static bool IsPath(string id)
            {
                return id.IndexOf('.') >= 0;
            }

            public string GetFullPath()
            {
                string output = id;
                Element traverser = parent;
                while(traverser != null)
                {
                    if(traverser.id != null && traverser.id.Length > 0)
                    {
                        output = traverser.id + "." + output;
                    }
                    traverser = traverser.parent;
                }
                return output;
            }

            public bool TryGetData<T>(out T data)
            {
                var isDataHolder = this as Element<T>;
                if(isDataHolder != null)
                {
                    data = isDataHolder.data;
                    return true;
                }
                else
                {
                    data = default(T);
                    return false;
                }
            }

            public virtual SerializedElementData ToSerializedFormat()
            {
                return new SerializedElementData(GetFullPath(), null, null);
            }

        }

        protected class Element<T> : Element, IDataElement 
        {
            public T data;

            [Serializable]
            public struct DataBox
            {
                public T m_data;
            }
            

            public Element(string id, T data, ContextLayeredDataStorage owner) : base(id,owner)
            {
                this.data = data;
            }


            public override SerializedElementData ToSerializedFormat()
            {
                try
                {
                    return new SerializedElementData(GetFullPath(), typeof(T).AssemblyQualifiedName, JsonUtility.ToJson(new DataBox() { m_data = data }, true));
                }
                catch
                {
                    Debug.LogError($"Could not serialize data associated with {GetFullPath()}: {data}");
                    return new SerializedElementData(GetFullPath(), typeof(T).AssemblyQualifiedName, null);
                }
            }
        }

        //Used to organize elements when serialized to try and keep a consistent ordering
        private class SerializedDataComparer : IComparer<SerializedElementData>
        {
            public int Compare(SerializedElementData x, SerializedElementData y)
            {
                return x.id.CompareTo(y.id);
            }
        }

        //Stores a single Element's data 
        [Serializable]
        public struct SerializedElementData
        {
            public string id;
            public string type;
            public string data;

            public SerializedElementData(string id, string type, string data)
            {
                this.id = id;
                this.type = type;
                this.data = data;
            }
        }

        //Stores a layers data
        [Serializable]
        public struct SerializedLayerData
        {
            public string layerName;
            public List<SerializedElementData> layerData;

            public SerializedLayerData(string layerName, List<SerializedElementData> layerData)
            {
                this.layerName = layerName;
                this.layerData = layerData;
            }
        }

        [Serializable]
        public struct LayerDescriptor
        {
            public string layerName;
            public bool isSerialized;

            public LayerDescriptor(string layerName, bool isSerialized = false)
            {
                this.layerName = layerName;
                this.isSerialized = isSerialized;
            }
        }

        protected class LayerList : SortedList<int, (LayerDescriptor descriptor, Element element)>
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
                Add(priority, (new LayerDescriptor(name, isSerialized), new Element<int>("", priority, owner)));
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

        [SerializeField]
        protected List<SerializedLayerData> m_serializedData;

        [NonSerialized]
        protected readonly LayerList m_layerList;
        [NonSerialized]
        protected Dictionary<string, Element> m_flatStructureLookup;

        public ContextLayeredDataStorage()
        {
            m_layerList = new LayerList(this);
            m_flatStructureLookup = new Dictionary<string, Element>();
            AddDefaultLayers();
        }

        //overridable default structure setup
        protected virtual void AddDefaultLayers()
        {
            m_layerList.AddLayer(-1, "Root");
        }

        public void AddNewTopLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                throw new System.ArgumentException("Cannor use a null or empty layer name", "layerName");
            }
            m_layerList.AddNewTopLayer(layerName);
        }

        //AddData with no specified layer gets added to the topmost layer
        protected void AddData<T>(string id, T data, out Element<T> elem)
        {
            AddData(m_layerList.GetTopLayerRoot(), id, data, out elem);
        }

        public IDataElement AddData<T>(string id, T data)
        {
            AddData(id, data, out Element<T> element);
            return element;
        }

        protected void AddData(string id, out Element elem)
        {
            AddData(m_layerList.GetTopLayerRoot(), id, out elem);
        }

        public IDataElement AddData(string id)
        {
            AddData(id, out Element elem);
            return elem;
        }

        protected void AddData<T>(LayerDescriptor layer, string id, T data, out Element<T> elem)
        {
            elem = null;
            Element root = m_layerList.GetLayerRoot(layer.layerName);
            if(root != null)
            {
                AddData(root, id, data, out elem);
            }
        }

        public IDataElement AddData<T>(string layer, string id, T data)
        {
            AddData(new LayerDescriptor() { layerName = layer }, id, data, out Element<T> elem);
            return elem;
        }

        protected void AddData(LayerDescriptor layer, string id, out Element elem)
        {
            elem = null;
            Element root = m_layerList.GetLayerRoot(layer.layerName);
            if(root != null)
            {
                AddData(root, id, out elem);
            }
        }

        public IDataElement AddData(LayerDescriptor layer, string id)
        {
            AddData(layer, id, out Element elem);
            return elem;
        }

        protected void AddData<T>(Element elem, string id, T data, out Element<T> output)
        {
            EvaluateParentAndId(in elem, id, out Element parent, out string newId);
            output = new Element<T>(newId, data, this);
            AddChild(parent, output);
            UpdateFlattenedStructureAdd(output);
        }

        protected void AddData(Element elem, string id, out Element output)
        {
            EvaluateParentAndId(in elem, id, out Element parent, out string newId);
            output = new Element(newId, this);
            AddChild(parent, output);
            UpdateFlattenedStructureAdd(output);
        }

        private void AddChild(Element parent, Element child)
        {
            parent.children.Add(child);
            child.parent = parent;
        }

        private void RemoveChild(Element parent, Element child)
        {
            parent.children.Remove(child);
            child.parent = null;
        }

        private void EvaluateParentAndId(in Element element, string id, out Element parent, out string newId)
        {
            parent = null;
            newId = null;
            if(!Element.IsPath(id))
            {
                parent = element;
                newId = id;
            }
            else
            {
                bool childFound = false;
                foreach (Element child in element.children)
                {
                    if (id.StartsWith(child.id) && id.Substring(child.id.Length).StartsWith("."))
                    {
                        EvaluateParentAndId(in child, id.Substring(child.id.Length).TrimStart('.'), out parent, out newId);
                        childFound = true;
                    }
                }
                if(!childFound)
                {
                    parent = element;
                    newId = id;
                }
            }
        }

        public void RemoveData(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Cannot remove a null or empty string id", "id");
            }
            var elem = SearchInternal(id);
            if(elem != null)
            {
                RemoveData(elem);
            }
        }

        protected void RemoveData(Element elem)
        {
            if(elem.id.Length == 0 && elem.parent == null)
            {
                throw new ArgumentException("Cannot remove the root element of a layer", "elem");
            }
            if (elem != null)
            {
                string fullPath = elem.GetFullPath();
                RemoveInternal(elem);
                UpdateFlattenedStructureRemove(fullPath);
            }
        }

        private void RemoveInternal(Element elem)
        {
            List<Element> childrenToRemove  = new List<Element>();
            foreach (var child in elem.children)
            {
                child.id = $"{elem.id}.{child.id}";
                if (elem.parent != null)
                {
                    childrenToRemove.Add(child);
                }
            }

            foreach(var child in childrenToRemove)
            {
                RemoveChild(elem, child);
                AddChild(elem.parent, child);
            }
            RemoveChild(elem.parent, elem);

        }

        protected void RemoveDataBranch(Element root)
        {
            GatherAll(root, out var elems);
            foreach(var elem in elems)
            {
                RemoveData(elem);
            }
            RemoveData(root);
        }

        public IDataElement Search(string lookup)
        {
            if(m_flatStructureLookup.TryGetValue(lookup, out Element reference))
            {
                return reference;
            }
            return null;
        }

        //Search layers in hierarchical order for the first element with the name lookup
        protected Element SearchInternal(string lookup)
        {
            foreach(var layer in m_layerList)
            {
                Element elem = SearchRecurse(layer.Value.element, lookup);
                if(elem != null)
                {
                    return elem;
                }
            }
            return null;
        }

        protected Element SearchRelative(Element elem, string lookup)
        {
            if(elem.children.Count > 0)
            {
                Element output = null;
                foreach(Element child in elem.children)
                {
                    output = SearchRecurse(child, lookup);
                    if(output != null)
                    {
                        return output;
                    }
                }
            }
            return null;
        }

        //may rewrite as non recursive
        protected Element SearchRecurse(Element elem, string lookup)
        {
            if (string.CompareOrdinal(elem.id, lookup) == 0)
            {
                return elem;
            }

            if(elem.id.Length == 0
            ||(lookup.StartsWith(elem.id) && lookup.Substring(elem.id.Length).StartsWith(".")))
            {
                Element output = null;
                string nextLookup = lookup.Substring(elem.id.Length).TrimStart('.');
                foreach(Element child in elem.children)
                {
                    output = SearchRecurse(child, nextLookup);
                    if(output != null)
                    {
                        return output;
                    }
                }
            }
            return null;
        }

        protected bool Contains(Element elem)
        {
            return elem.owner == this;
        }

        protected void Rebalance()
        {
            foreach(var layer in m_layerList)
            {
                GatherAll(layer.Value.element, out var elems);
                Rebalance(layer.Value.element, elems);
            }
        }

        //Get all elements descended from root inclusive in a list
        protected void GatherAll(Element root, out List<Element> elements)
        {
            elements = new List<Element>();
            foreach(var child in root.children)
            {
                elements.Add(child);
                GatherAll(child, out var accumulatedElements);
                elements.AddRange(accumulatedElements);
            }
        }

        /// <summary>
        /// A recursive function that takes in a root element, and any elements that have that root as a parent/ancestor.
        /// The function figures out and hooks up the direct children of the root, then recurses on each child with a list of
        /// elements that share a path with the child's name.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="elementsWithSharedRoot"></param>
        protected void Rebalance(Element root, IEnumerable<Element> elementsWithSharedRoot)
        {
            List<(Element potentialRoot, List<Element> potentialShared)> recurseList = new List<(Element potentialRoot, List<Element> potentialShared)>();

            //Sort all elements into buckets with a root element
            foreach (var element in elementsWithSharedRoot)
            {
                bool found = false;
                bool subsumed = false;
                int i;
                //check to see if this element's name would serve as a better root for any existing bucket, or if it belongs in an existing bucket
                for (i = 0; i < recurseList.Count; ++i)
                {
                    var (potentialRoot, potentialShared) = recurseList[i];
                    if (potentialRoot.id.StartsWith(element.id) && potentialRoot.id.Substring(element.id.Length).StartsWith("."))
                    {
                        found = true;
                        subsumed = true;
                        potentialShared.Add(potentialRoot);
                        recurseList[i] = (element, potentialShared);
                        break;
                    }
                    else if (element.id.StartsWith(potentialRoot.id) && element.id.Substring(potentialRoot.id.Length).StartsWith("."))
                    {
                        found = true;
                        potentialShared.Add(element);
                        break;
                    }
                }

                if(subsumed)
                {
                    //If we found a new root, it is possible an existing bucket may get subsumed by the new root.
                    List<(Element, List<Element>)> colapsed = new List<(Element, List<Element>)>();
                    var (foundRoot, currentList) = recurseList[i];
                    foreach(var (potentialRoot, potentialShared) in recurseList)
                    {
                        if(foundRoot != potentialRoot && potentialRoot.id.StartsWith(foundRoot.id) && potentialRoot.id.Substring(foundRoot.id.Length).StartsWith("."))
                        {
                            currentList.AddRange(potentialShared);
                            currentList.Add(potentialRoot);
                            colapsed.Add((potentialRoot, potentialShared));
                        }
                    }

                    foreach(var c in colapsed)
                    {
                        recurseList.Remove(c);
                    }
                }
                //if this element did not belong in any existing bucket, create a new bucket 
                else if (!found)
                {
                    recurseList.Add((element, new List<Element>()));
                }
            }

            //Recurse on buckets
            foreach (var (nextRoot, nextList) in recurseList)
            {
                if(nextRoot.parent != null)
                {
                    RemoveChild(nextRoot.parent, nextRoot);
                }
                AddChild(root, nextRoot);
                foreach(var elem in nextList)
                {
                    elem.id = elem.id.Substring(nextRoot.id.Length + 1);
                }
                Rebalance(nextRoot, nextList);
            }

        }

        public void OnBeforeSerialize()
        {
            m_serializedData = new List<SerializedLayerData>();
            foreach(var layer in m_layerList)
            {
                if(layer.Value.descriptor.isSerialized)
                {
                    var serializedLayer = new SerializedLayerData(layer.Value.descriptor.layerName, new List<SerializedElementData>());
                    GatherAll(layer.Value.element, out var elements);
                    foreach(var elem in elements)
                    {
                        serializedLayer.layerData.Add(elem.ToSerializedFormat());
                    }
                    serializedLayer.layerData.Sort(new SerializedDataComparer());
                    m_serializedData.Add(serializedLayer);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(var serializedLayer in m_serializedData)
            {
                var root = m_layerList.GetLayerRoot(serializedLayer.layerName);
                List<Element> elems = new List<Element>();
                foreach (var data in serializedLayer.layerData)
                {
                    elems.Add(DeserializeElement(data));
                }
                Rebalance(root, elems);
                foreach(var elem in elems)
                {
                    UpdateFlattenedStructureAdd(elem);
                }
            }
        }

        private Element DeserializeElement(SerializedElementData data)
        {
            if (data.type != null && data.type.Length > 0)
            {
                try
                {
                    Type generic = typeof(Element<>);
                    Type holderGeneric = typeof(Element<>.DataBox);

                    //This is the line that will fail in this try catch - when we have no idea what the type string resolves to
                        Type dataType = Type.GetType(data.type);
                    //---------------------------------------------------------------------------------------------------------

                    Type constructed = generic.MakeGenericType(dataType);
                    Type holderConstructed = holderGeneric.MakeGenericType(dataType);

                    var constructor = constructed.GetConstructor(new Type[] {typeof(string), dataType, typeof(ContextLayeredDataStorage) });
                    FieldInfo saved = holderConstructed.GetField("m_data");
                    object value = saved.GetValue(JsonUtility.FromJson(data.data, holderConstructed));
                    Element elem = (Element)constructor.Invoke(new object[] {data.id, Convert.ChangeType(value, dataType), this });
                    return elem;
                }
                catch
                {
                    Debug.LogError($"Could not deserialize the data on element {data.id} of type {data.type}");
                }
            }
            return new Element(data.id, this);
        }

        protected void UpdateFlattenedStructureAdd(Element addedElement)
        {
            string id = addedElement.GetFullPath();
            if(m_flatStructureLookup.TryGetValue(id, out Element elem))
            {
                if(GetHierarchyValue(addedElement) > GetHierarchyValue(elem))
                {
                    m_flatStructureLookup[id] = addedElement;
                }
            }
            else
            {
                m_flatStructureLookup.Add(id, addedElement);
            }
        }

        protected void UpdateFlattenedStructureRemove(string removedElementId)
        {
            var replacement = SearchInternal(removedElementId);
            if(replacement != null)
            {
                m_flatStructureLookup[removedElementId] = replacement;
            }
            else
            {
                m_flatStructureLookup.Remove(removedElementId);
            }
        }

        private int GetHierarchyValue(Element element)
        {
            if(element.owner != this)
            {
                Debug.LogError("Tried to get hierarchy value on an element not registered to this store: " + element.id);
                return int.MinValue;
            }

            Element traverser = element;
            while(traverser.parent != null)
            {
                traverser = traverser.parent;
            }

            //our root elements have no name and an int value signifying the layer value
            if(traverser is Element<int> root && (root.id == null || root.id.Length == 0))
            {
                return root.data;
            }
            else
            {
                //something very strange happened, we have an orphaned root somehow...unsure what we should do if this ever gets hit
                Debug.LogError("How did we reach this state???");
                return int.MinValue;
            }
        }
    }
}
