using System;
using System.Collections.Generic;
using System.Reflection;
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
        protected Dictionary<ElementID, Element> m_flatStructureLookup;
        [NonSerialized]
        protected Element m_flatStructure;
        public IEnumerable<(string layerName, Element root)> LayerList
        {
            get
            {
                foreach(var (_, data) in m_layerList)
                {
                    yield return (data.descriptor.layerName, data.element);
                }
            }
        }

        public IEnumerable<(ElementID, Element)> FlatStructureLookup
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
            m_flatStructureLookup = new Dictionary<ElementID, Element>(new ElementIDComparer());
            m_flatStructure = new Element<int>("", -1, this);
            m_metadata = new MetadataCollection(this);
            AddDefaultLayers();
        }

        public bool HasMetadata(ElementID id, string lookup)
        {
            return m_metadata.TryGetValue(id, out MetadataBlock block) && block.HasMetadata(lookup);
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
                throw new System.ArgumentException("Cannot use a null or empty layer name", "layerName");
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
            // Waste time
            for (int i = 0; i < 10000000; ++i)
            {
                var x = i * 10;
                Debug.Log(x);
            }

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

        private void AddChildUnique(Element parent, ref Element child)
        {
            child.ID = child.GetUniqueLocalID(child.ID.FullPath.Replace($"{parent.ID.FullPath}.", ""), parent);
            AddChild(parent, child);
        }

        private void RemoveChild(Element parent, Element child)
        {
            parent.Children.Remove(child);
            child.Parent = null;
        }

        private void EvaluateParent(in Element element, ElementID id, out Element parent)
        {
            parent = null;
            Element traverser = element;
            Element swap = null;
            bool found = true;
            while(found)
            {
                found = false;
                foreach(Element child in traverser.Children)
                {
                    if(child.ID.IsSubpathOf(id))
                    {
                        swap = child;
                        found = true;
                        break;
                    }
                }

                if(found)
                {
                    traverser = swap;
                    swap = null;
                }
                else
                {
                    parent = traverser;
                }
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
                recurse.ID = ElementID.FromString($"{dst.ID.FullPath}{elem.ID.FullPath.Replace(src.ID.FullPath, "")}");
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
            if(m_flatStructureLookup.TryGetValue(lookup, out Element reference))
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
                Element elem = SearchRelative(layer.Value.element, lookup);
                if(elem != null)
                {
                    return elem;
                }
            }
            return null;
        }

        public Element SearchRelative(Element elem, ElementID lookup)
        {
            const int stackSize = 128;
            Stack<Element> workingSet = new Stack<Element>(stackSize);
            workingSet.Push(elem);
            while (workingSet.Count > 0)
            {
                var current = workingSet.Pop();
                foreach (var child in current.Children)
                {
                    if(child.ID.Equals(lookup))
                    {
                        return child;
                    }
                    else if (child.ID.IsSubpathOf(lookup))
                    {
                        workingSet.Push(child);
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
                GatherAll(child, out var accumulatedElements);
                elements.AddRange(accumulatedElements);
                elements.Add(child);
            }
        }

        protected void GatherAll(DataReader root, out List<DataReader> readers)
        {
            readers = new List<DataReader>();
            foreach(var element in root.Element.Children)
            {
                var reader = element.GetReader();
                readers.Add(reader);
                GatherAll(reader, out var accumulatedReaders);
                readers.AddRange(accumulatedReaders);
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
                    var serializedLayer = SerializeLayer(layer.Value.descriptor, layer.Value.element);
                    m_serializedData.Add(serializedLayer);
                }
            }
        }

        internal SerializedLayerData SerializeLayer(LayerDescriptor layerDescriptor, Element root)
        {
            var serializedLayer = new SerializedLayerData(layerDescriptor.layerName, new List<SerializedElementData>());
            GatherAll(root, out var elements);
            foreach (var elem in elements)
            {
                serializedLayer.layerData.Add(elem.ToSerializedFormat());
            }
            serializedLayer.layerData.Sort(new SerializedDataComparer());
            return serializedLayer;
        }

        public virtual (string layer, string metadata) CopyElementCollection(IEnumerable<DataReader> readers)
        {
            var serializedLayer = new SerializedLayerData("CopyLayer", new List<SerializedElementData>());
            var meta = new MetadataCollection(this);
            foreach (var reader in readers)
            {
                var elem = reader.Element;
                serializedLayer.layerData.Add(elem.ToSerializedFormat());
                if(m_metadata.TryGetValue(elem.ID.FullPath, out var block))
                {
                    meta.Add(elem.ID.FullPath, block);
                }
            }
            serializedLayer.layerData.Sort(new SerializedDataComparer());
            return (EditorJsonUtility.ToJson(serializedLayer, true), EditorJsonUtility.ToJson(meta, true));
        }

        public virtual IEnumerable<DataReader> PasteElementCollection(string serializedLayer, string serializedMetadata, string layerToPasteTo, out Dictionary<string, string> remappings)
        {
            //WIP, need to work through the logic of pasting into an existing layer
            var pasteLayer = new SerializedLayerData();
            EditorJsonUtility.FromJsonOverwrite(serializedLayer, pasteLayer);

            var meta = new MetadataCollection(this);
            EditorJsonUtility.FromJsonOverwrite(serializedMetadata, meta);

            var root = m_layerList.GetLayerRoot(layerToPasteTo);
            remappings = new Dictionary<string, string>();
            List<DataReader> addedElements = new List<DataReader>();
            foreach (var elemData in pasteLayer.layerData)
            {
                var elem = DeserializeElement(elemData);
                MetadataBlock block = null;
                meta.TryGetValue(elem.ID.FullPath, out block);
                foreach (var remap in remappings)
                {
                    elem.ID = elem.ID.Rename(remap.Key, remap.Value);
                }
                var initialID = elem.ID.LocalPath;
                EvaluateParent(in root, elem.ID, out var parent);
                AddChildUnique(parent, ref elem);
                if(elem.ID.LocalPath != initialID)
                {
                    remappings.Add(initialID, elem.ID.LocalPath);
                }
                UpdateFlattenedStructureAdd(elem);
                if(block != null)
                {
                    m_metadata.Add(elem.ID.FullPath, block);
                }
                addedElements.Add(elem.GetReader());
            }
            return addedElements;
        }

        public virtual void OnAfterDeserialize()
        {
            foreach(var serializedLayer in m_serializedData)
            {
                DeserializeLayer(serializedLayer);
            }
        }

        //This version expects a layer in the layerlist to have a matching name to the serializedLayerData
        private void DeserializeLayer(SerializedLayerData data)
        {
            var root = m_layerList.GetLayerRoot(data.layerName);
            List<Element> elems = new List<Element>();
            foreach (var elemData in data.layerData)
            {
                elems.Add(DeserializeElement(elemData));
            }
            Rebalance(root, elems);
            foreach (var elem in elems)
            {
                UpdateFlattenedStructureAdd(elem);
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
                    output = new Element(ElementID.FromString(data.id), this);
                }
            }
            else
            {
                output = new Element(ElementID.FromString(data.id), this);
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

        private IEnumerable<Element> FlatStructurePartialSearch(ElementID searchID)
        {
            Stack<Element> workingSet = new Stack<Element>();
            HashSet<ElementID> returnedElements = new HashSet<ElementID>(new ElementIDComparer());
            workingSet.Push(m_flatStructure);
            while(workingSet.Count > 0)
            {
                var current = workingSet.Pop();
                foreach(var child in current.Children)
                {
                    if(searchID.IsImmediateSubpathOf(child.ID))
                    {
                        returnedElements.Add(child.ID);
                    }
                    else if(child.ID.IsSubpathOf(searchID) || child.ID.Equals(searchID))
                    {
                        workingSet.Push(child);
                    }
                }
            }

            foreach(var cid in returnedElements)
            {
                yield return m_flatStructureLookup[cid];
            }
        }

        internal IEnumerable<Element> GetChildren(ElementID id)
        {
            //var search = SearchRelative(m_flatStructure, id);
            //if (search != null)
            //{
            //    foreach (var c in search.Children)
            //    {
            //        children.Add(c.ID); //the partial search might already encompass this and the initial search doesnt need to happen
            //    }
            //}
            foreach(var c in FlatStructurePartialSearch(id))
            {
                yield return m_flatStructureLookup[c.ID];
            }
        }

        internal void UpdateFlattenedStructureAdd(Element addedElement)
        {
            ElementID id = addedElement.ID;
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
                EvaluateParent(in m_flatStructure, id, out Element parent);
                AddChild(parent, new Element(id, this));
            }
        }

        protected void UpdateFlattenedStructureRemove(ElementID removedElementId)
        {
            var replacement = SearchInternal(removedElementId);
            if(replacement != null)
            {
                m_flatStructureLookup[removedElementId] = replacement;
            }
            else
            {
                m_metadata.Remove(removedElementId.FullPath);
                m_flatStructureLookup.Remove(removedElementId);
                var e = SearchRelative(m_flatStructure, removedElementId);
                if(e != null)
                {
                    RemoveInternal(e);
                }
            }
        }

        internal int GetHierarchyValue(Element element)
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
