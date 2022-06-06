using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    [Serializable]
    public class ContextLayeredDataStorage : ISerializationCallbackReceiver
    {
        [SerializeField]
        internal List<SerializedLayerData> m_serializedData;
        [SerializeField]
        internal MetadataCollection m_metadata;
        [NonSerialized]
        internal readonly LayerList m_layerList;
        [NonSerialized]
        protected Dictionary<string, Element> m_flatStructureLookup;
        protected IEnumerable<(string layerName, Element root)> LayerList
        {
            get
            {
                foreach(var (_, data) in m_layerList)
                {
                    yield return (data.descriptor.layerName, data.element);
                }
            }
        }

        public IEnumerable<(string, Element)> FlatStructureLookup
        {
            get
            {
                foreach(var (key, value) in m_flatStructureLookup)
                {
                    yield return (key, value);
                }
            }
        }

        public ContextLayeredDataStorage()
        {
            m_layerList = new LayerList(this);
            m_flatStructureLookup = new Dictionary<string, Element>();
            m_metadata = new MetadataCollection();
            AddDefaultLayers();
        }

        public bool HasMetadata(ElementID id, string lookup)
        {
            return m_metadata.TryGetValue(id.FullPath, out MetadataBlock block) && block.HasMetadata(lookup);
        }

        public T GetMetadata<T>(ElementID id, string lookup)
        {
            if(m_metadata.TryGetValue(id.FullPath, out MetadataBlock block))
            {
                return block.GetMetadata<T>(lookup);
            }
            return default;
        }

        public void SetMetadata<T>(ElementID id, string lookup, T data)
        {
            MetadataBlock block;
            if(!m_metadata.TryGetValue(id.FullPath, out block))
            {
                block = new MetadataBlock();
                m_metadata.Add(id.FullPath, block);
            }
            block.SetMetadata(lookup, data);
        }

        //overridable default structure setup
        protected virtual void AddDefaultLayers()
        {
            m_layerList.AddLayer(-1, "Root");
        }

        protected void AddLayer(int priority, string layerName, bool isSerialized = false)
        {
            m_layerList.AddLayer(priority, layerName, isSerialized);
        }

        public void AddNewTopLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                throw new System.ArgumentException("Cannor use a null or empty layer name", "layerName");
            }
            m_layerList.AddNewTopLayer(layerName);
        }

        protected void SetHeader(ElementID id, DataHeader header)
        {
            Search(id).Element.Header = header;
        }

        protected void SetHeader(Element element, DataHeader header)
        {
            element.Header = header;
        }

        protected Element GetLayerRoot(string layerName)
        {
            return m_layerList.GetLayerRoot(layerName);
        }

        protected Element GetElementFromLayer(string layerName, ElementID id)
        {
            Element root = GetLayerRoot(layerName);
            if (root == null)
            {
                return null;
            }
            else
            {
                return SearchRelative(root, id);
            }
        }

        protected Element AddElementToLayer(string layerName, ElementID id)
        {
            Element root = GetLayerRoot(layerName);
            if (root == null)
            {
                return null;
            }
            else
            {
                AddData(root, id, out Element element);
                return element;
            }
        }

        protected Element<T> AddElementToLayer<T>(string layerName, ElementID id, T data)
        {
            Element root = GetLayerRoot(layerName);
            if (root == null)
            {
                return null;
            }
            else
            {
                AddData(root, id, data, out Element<T> element);
                return element;
            }
        }


        //AddData with no specified layer gets added to the topmost layer
        internal void AddData<T>(ElementID id, T data, out Element<T> elem)
        {
            AddData(m_layerList.GetTopLayerRoot(), id, data, out elem);
        }

        public DataWriter AddData<T>(ElementID id, T data)
        {
            AddData(id, data, out Element<T> element);
            return element.GetWriter();
        }

        internal void AddData(ElementID id, out Element elem)
        {
            AddData(m_layerList.GetTopLayerRoot(), id, out elem);
        }

        public DataWriter AddData(ElementID id)
        {
            AddData(id, out Element element);
            return element.GetWriter();
        }

        internal void AddData<T>(LayerDescriptor layer, ElementID id, T data, out Element<T> elem)
        {
            elem = null;
            Element root = m_layerList.GetLayerRoot(layer.layerName);
            if(root != null)
            {
                AddData(root, id, data, out elem);
            }
        }

        public DataWriter AddData<T>(string layer, ElementID id, T data)
        {
            AddData(new LayerDescriptor() { layerName = layer }, id, data, out Element<T> element);
            return element.GetWriter();
        }


        internal void AddData<T>(Element elem, ElementID id, T data, out Element<T> output)
        {
            EvaluateParent(in elem, id, out Element parent);
            output = new Element<T>(id, data, this);
            AddChild(parent, output);
            UpdateFlattenedStructureAdd(output);
        }

        internal void AddData(Element elem, ElementID id, out Element output)
        {
            EvaluateParent(in elem, id, out Element parent);
            output = new Element(id, this);
            AddChild(parent, output);
            UpdateFlattenedStructureAdd(output);
        }

        private void AddChild(Element parent, Element child)
        {
            parent.Children.Add(child);
            child.Parent = parent;
        }

        private void RemoveChild(Element parent, Element child)
        {
            parent.Children.Remove(child);
            child.Parent = null;
        }

        private void EvaluateParent(in Element element, ElementID id, out Element parent)
        {
            parent = null;
            bool childFound = false;
            foreach (Element child in element.Children)
            {
                if (child.ID.IsSubpathOf(id))
                {
                    EvaluateParent(in child, id, out parent);
                    childFound = true;
                }
            }
            if(!childFound)
            {
                parent = element;
            }
        }

        public void RemoveData(ElementID id)
        {
            var elem = SearchInternal(id);
            if(elem != null)
            {
                RemoveData(elem);
            }
        }

        internal void RemoveData(Element elem)
        {
            if(elem.ID.FullPath.Length == 0 && elem.Parent == null)
            {
                throw new ArgumentException("Cannot remove the root element of a layer", "elem");
            }
            if (elem != null)
            {
                var id = elem.ID;
                RemoveInternal(elem);
                UpdateFlattenedStructureRemove(id);
            }
        }

        internal void RemoveData(Element elem, ElementID id)
        {
            var e = SearchRelative(elem, id);
            if(e != null)
            {
                RemoveData(e);
            }
        }

        private void RemoveInternal(Element elem)
        {
            List<Element> childrenToRemove  = new List<Element>();
            foreach (var child in elem.Children)
            {
                if (elem.Parent != null)
                {
                    childrenToRemove.Add(child);
                }
            }

            foreach(var child in childrenToRemove)
            {
                RemoveChild(elem, child);
                AddChild(elem.Parent, child);
            }
            RemoveChild(elem.Parent, elem);

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

        protected void CopyDataBranch(Element src, Element dst)
        {
            foreach(var elem in src.Children)
            {
                var recurse = elem.MakeCopy();
                recurse.ID = $"{dst.ID.FullPath}{elem.ID.FullPath.Replace(src.ID.FullPath, "")}";
                AddChild(dst,recurse);
                UpdateFlattenedStructureAdd(recurse);
                CopyDataBranch(elem, recurse);
            }
        }

        //Liz:This breaks our contract in having the readers and writers directly acess their elements, but
        // to unblock work we will allow this for now
        public void CopyDataBranch(DataReader src, DataWriter dst)
        {
            CopyDataBranch(src.Element, dst.Element);
        }

        public DataReader Search(ElementID lookup)
        {
            if(m_flatStructureLookup.TryGetValue(lookup.FullPath, out Element reference))
            {
                return reference.GetReader();
            }
            return null;
        }

        //Search layers in hierarchical order for the first element with the name lookup
        protected Element SearchInternal(ElementID lookup)
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

        protected Element SearchRelative(Element elem, ElementID lookup)
        {
            if(elem.Children.Count > 0)
            {
                Element output = null;
                foreach(Element child in elem.Children)
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
        internal Element SearchRecurse(Element elem, ElementID lookup)
        {
            if (elem.ID.Equals(lookup))
            {
                return elem;
            }

            if(elem.ID.FullPath.Length == 0
            || elem.ID.IsSubpathOf(lookup))
            {
                Element output = null;
                foreach(Element child in elem.Children)
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

        internal bool Contains(Element elem)
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
        internal void GatherAll(Element root, out List<Element> elements)
        {
            elements = new List<Element>();
            foreach(var child in root.Children)
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
        internal void Rebalance(Element root, IEnumerable<Element> elementsWithSharedRoot)
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
                    if (element.ID.IsSubpathOf(potentialRoot.ID))
                    {
                        found = true;
                        subsumed = true;
                        potentialShared.Add(potentialRoot);
                        recurseList[i] = (element, potentialShared);
                        break;
                    }
                    else if (potentialRoot.ID.IsSubpathOf(element.ID))
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
                        if(foundRoot != potentialRoot && potentialRoot.ID.IsSubpathOf(foundRoot.ID))
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
                if(nextRoot.Parent != null)
                {
                    RemoveChild(nextRoot.Parent, nextRoot);
                }
                AddChild(root, nextRoot);
                Rebalance(nextRoot, nextList);
            }

        }

        public virtual void OnBeforeSerialize()
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

        public virtual void OnAfterDeserialize()
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
            Element output = null;
            if (data.valueType != null && data.valueType.Length > 0)
            {
                try
                {
                    Type generic = typeof(Element<>);
                    Type holderGeneric = typeof(Element<>.DataBox);

                    //This is the line that will fail in this try catch - when we have no idea what the type string resolves to
                        Type dataType = Type.GetType(data.valueType);
                    //---------------------------------------------------------------------------------------------------------

                    Type constructed = generic.MakeGenericType(dataType);
                    Type holderConstructed = holderGeneric.MakeGenericType(dataType);

                    var constructor = constructed.GetConstructor(new Type[] {typeof(ElementID), dataType, typeof(ContextLayeredDataStorage) });
                    FieldInfo saved = holderConstructed.GetField("m_data");
                    object value = saved.GetValue(JsonUtility.FromJson(data.valueData, holderConstructed));
                    output = (Element)constructor.Invoke(new object[] {ElementID.FromString(data.id), Convert.ChangeType(value, dataType), this });
                }
                catch
                {
                    Debug.LogError($"Could not deserialize the data on element {data.id} of type {data.valueType}");
                    output = new Element(data.id, this);
                }
            }
            else
            {
                output = new Element(data.id, this);
            }

            try
            {
                Type headerType = Type.GetType(data.headerType);
                object obj = Activator.CreateInstance(headerType);
                DataHeader header = obj as DataHeader;
                header.FromJson(data.headerData);
                output.Header = header;
            }
            catch
            {
                Debug.LogError($"Could not deserialize the header on element {data.id} of type {data.headerType}");
                output.Header = new DataHeader();
            }
            return output;
        }

        internal void UpdateFlattenedStructureAdd(Element addedElement)
        {
            string id = addedElement.ID.FullPath;
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

        protected void UpdateFlattenedStructureRemove(ElementID removedElementId)
        {
            var replacement = SearchInternal(removedElementId);
            if(replacement != null)
            {
                m_flatStructureLookup[removedElementId.FullPath] = replacement;
            }
            else
            {
                m_flatStructureLookup.Remove(removedElementId.FullPath);
            }
        }

        private int GetHierarchyValue(Element element)
        {
            if(element.owner != this)
            {
                Debug.LogError("Tried to get hierarchy value on an element not registered to this store: " + element.ID);
                return int.MinValue;
            }

            Element traverser = element;
            while(traverser.Parent != null)
            {
                traverser = traverser.Parent;
            }

            //our root elements have no name and an int value signifying the layer value
            if(traverser is Element<int> root && (root.ID.FullPath.Length == 0))
            {
                return root.Data;
            }
            else
            {
                //something very strange happened, we have an orphaned root somehow...unsure what we should do if this ever gets hit
                Debug.LogError("How did we reach this state???");
                return int.MinValue;
            }
        }

        internal IEnumerable<Element> GetFlatImmediateChildList(Element root)
        {
            foreach (var (key, value) in FlatStructureLookup)
            {
                if (root.ID.IsImmediateSubpathOf(key))
                {
                    yield return value;
                }
            }

        }
    }
}
