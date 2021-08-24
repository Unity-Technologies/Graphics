using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphStorage : ContextLayeredDataStorage.ContextLayeredDataStorage
    {
        private class GraphReader : IDisposable, INodeReader, IPortReader, IFieldReader, IDataReader
        {

            private bool HasSubData(Element element, params string[] expectedChildren)
            {
                foreach(string childName in expectedChildren)
                {
                    var check = GetSubData(element, childName);
                    if(check == null)
                    {
                        return false;
                    }
                }
                return true;
            }

            private Element GetSubData(Element element, string dataID)
            {
                foreach(var child in element.children)
                {
                    if(child.id.CompareTo(dataID) == 0)
                    {
                        return child;
                    }
                }
                return null;
            }

            private bool IsPortReader(Element element)
            {
                return HasSubData(element, "_isInput", "_isHorizontal");
            }

            public WeakReference<Element<Element>> elementReference;
            public GraphStorage storageReference;
            public void Dispose()
            {
                elementReference = null;
                storageReference = null;
            }

            public GraphReader(Element element, GraphStorage storage)
            {
                this.elementReference = new WeakReference<Element<Element>>(storage.m_flatStructureLookup[element.GetFullPath()]);
                this.storageReference = storage;
            }

            private IEnumerable<Element> GetSubElements()
            {
                if (elementReference.TryGetTarget(out Element<Element> element))
                {
                    return element.children;
                }
                return null;
            }

            public IEnumerator<GraphReader> GetChildren()
            {
                foreach (var subElement in GetSubElements())
                {
                    yield return new GraphReader(subElement, this.storageReference);
                }
            }

            public bool TryGetValue<T>(out T value)
            {
                if (elementReference.TryGetTarget(out Element<Element> element) && element.data is Element<T> typeElement)
                {
                    value = typeElement.data;
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            private bool TryGetSubReader(string searchKey, out GraphReader graphReader)
            {
                if (elementReference.TryGetTarget(out Element<Element> element))
                {
                    Element maybeElement = storageReference.SearchRelative(element, searchKey);
                    if (maybeElement != null)
                    {
                        graphReader = new GraphReader(maybeElement, this.storageReference);
                        return true;
                    }
                }
                graphReader = null;
                return false;
            }

            private IEnumerable<GraphReader> GetSubReaders()
            {
                foreach (var subElement in GetSubElements())
                {
                    yield return new GraphReader(subElement, this.storageReference);
                }
            }

            #region interfaceImplementations

            public string GetName()
            {
                elementReference.TryGetTarget(out var element);
                return element.id;
            }

            public IEnumerable<IPortReader> GetPorts()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (IsPortReader(subElement))
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            public IEnumerable<IPortReader> GetInputPorts()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (GetSubData(subElement, "_isInput") is Element<bool> isInput && isInput.data)
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            public IEnumerable<IPortReader> GetOutputPorts()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (GetSubData(subElement, "_isInput") is Element<bool> isInput && !isInput.data)
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            IEnumerable<IFieldReader> INodeReader.GetFields()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (!IsPortReader(subElement))
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            public IEnumerable<IFieldReader> GetFields() => GetSubReaders();

            public bool TryGetPort(string portKey, out IPortReader portReader)
            {
                if (elementReference.TryGetTarget(out Element<Element> element))
                {
                    Element maybePort = storageReference.SearchRelative(element, portKey);
                    if (IsPortReader(maybePort))
                    {
                        portReader = new GraphReader(maybePort, this.storageReference);
                        return true;
                    }
                }
                portReader = null;
                return false;
            }

            bool INodeReader.TryGetField(string fieldKey, out IFieldReader fieldReader)
            {
                if (elementReference.TryGetTarget(out Element<Element> element))
                {
                    Element maybeField = storageReference.SearchRelative(element, fieldKey);
                    if (!IsPortReader(maybeField))
                    {
                        fieldReader = new GraphReader(maybeField, this.storageReference);
                        return true;
                    }
                }
                fieldReader = null;
                return false;
            }

            public bool TryGetField(string fieldKey, out IFieldReader fieldReader)
            {
                bool output = TryGetSubReader(fieldKey, out GraphReader reader);
                fieldReader = reader;
                return output;
            }
            public bool TryGetSubField(string fieldKey, out IFieldReader fieldReader) => TryGetField(fieldKey, out fieldReader);

            public IEnumerable<IPortReader> GetConnectedPorts()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IFieldReader> GetSubFields() => GetFields();

            public bool IsInput()
            {
                if(TryGetField("_isInput", out var reader) && reader.TryGetValue(out bool isInput))
                {
                    return isInput;
                }
                return false;
            }

            public bool IsHorizontal()
            {
                if (TryGetField("_isHorizontal", out var reader) && reader.TryGetValue(out bool isInput))
                {
                    return isInput;
                }
                return false;
            }

            #endregion

        }

        private class GraphWriter : IDisposable, INodeWriter, IPortWriter, IFieldWriter
        {

            public WeakReference<Element> elementReference;
            public GraphStorage storageReference;
            public void Dispose()
            {
                elementReference = null;
                storageReference = null;
            }

            public GraphWriter(Element element, GraphStorage storage)
            {
                elementReference = new WeakReference<Element>(element);
                storageReference = storage;
            }

            private GraphReader GetCorrespondingReader()
            {
                if(elementReference.TryGetTarget(out var elem))
                {
                    return new GraphReader(elem, storageReference);
                }
                return null;
            }

            private bool TryAddSubWriter(string key, out GraphWriter graphWriter)
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    storageReference.AddData(element, key, out Element addedData);
                    if (addedData != null)
                    {
                        graphWriter = new GraphWriter(addedData, this.storageReference);
                        return true;
                    }
                }
                graphWriter = null;
                return false;
            }

            private bool TryAddSubWriter<T>(string key, out GraphWriter<T> graphWriter) where T : ISerializable
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    storageReference.AddData(element, key, default, out Element<T> addedData);
                    if (addedData != null)
                    {
                        graphWriter = new GraphWriter<T>(addedData, this.storageReference);
                        return true;
                    }
                }
                graphWriter = null;
                return false;
            }

            private bool TryGetSubWriter(string key, out GraphWriter graphWriter)
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    var subElement = storageReference.SearchRelative(element, key);
                    if (subElement != null)
                    {
                        graphWriter = new GraphWriter(subElement, this.storageReference);
                        return true;
                    }
                }
                graphWriter = null;
                return false;
            }

            private bool TryGetSubWriter<T>(string key, out GraphWriter<T> graphWriter) where T : ISerializable
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    var subElement = storageReference.SearchRelative(element, key);
                    if (subElement != null && subElement is Element<T> typedSubElement)
                    {
                        graphWriter = new GraphWriter<T>(typedSubElement, this.storageReference);
                        return true;
                    }
                }
                graphWriter = null;
                return false;
            }



            #region interfaceImplementations
            public bool TryAddConnection(IPortWriter other)
            {
                GraphWriter otherWriter = other as GraphWriter;
                var thisReader = GetCorrespondingReader();
                var otherReader = otherWriter.GetCorrespondingReader();
                if (other != null && thisReader != null && otherReader != null && thisReader.IsInput() != otherReader.IsInput() && thisReader.IsHorizontal() == otherReader.IsHorizontal())
                {
                    if (elementReference.TryGetTarget(out Element element) && otherWriter.elementReference.TryGetTarget(out Element otherElement))
                    {
                        storageReference.LinkData(element, otherElement);
                        return true;
                    }
                }
                return false;
            }

            public bool TryAddField<T>(string fieldKey, out IFieldWriter<T> fieldWriter) where T : ISerializable
            {
                var output = TryAddSubWriter(fieldKey, out GraphWriter<T> graphWriter);
                fieldWriter = graphWriter;
                return output;
            }

            public bool TryAddField(string fieldKey, out IFieldWriter fieldWriter)
            {
                var output = TryAddSubWriter(fieldKey, out GraphWriter graphWriter);
                fieldWriter = graphWriter;
                return output;
            }

            public bool TryAddPort(string portKey, bool isInput, bool isHorizontal, out IPortWriter portWriter)
            {
                if(TryAddSubWriter(portKey, out GraphWriter graphWriter) && graphWriter.elementReference.TryGetTarget(out Element elem))
                {
                    storageReference.AddData<bool>(elem, "_isInput", isInput, out var _);
                    storageReference.AddData<bool>(elem, "_isHorizontal", isHorizontal, out var _);
                    portWriter = graphWriter;
                    return true;
                }
                else
                {
                    portWriter = null;
                    return false;
                }
            }

            public bool TryAddSubField(string subFieldKey, out IFieldWriter subFieldWriter) => TryAddField(subFieldKey, out subFieldWriter);

            public bool TryAddSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) where T : ISerializable => TryAddField<T>(subFieldKey, out subFieldWriter);

            public bool TryAddTransientNode<T>(string transientNodeKey, IFieldWriter<T> fieldWriter) where T : ISerializable
            {
                throw new NotImplementedException();
            }

            public bool TryGetField<T>(string fieldKey, out IFieldWriter<T> fieldWriter) where T : ISerializable
            {
                bool output = true;
                if(!TryGetSubWriter(fieldKey, out GraphWriter<T> subWriter))
                {
                    output = TryAddSubWriter(fieldKey, out subWriter);
                }
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetField(string fieldKey, out IFieldWriter fieldWriter)
            {
                bool output = true;
                if (!TryGetSubWriter(fieldKey, out GraphWriter subWriter))
                {
                    output = TryAddSubWriter(fieldKey, out subWriter);
                }
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetPort(string portKey, out IPortWriter portWriter)
            {
                bool output = true;
                if (!TryGetSubWriter(portKey, out GraphWriter subWriter))
                {
                    output = TryAddSubWriter(portKey, out subWriter);
                }
                portWriter = subWriter;
                return output;
            }

            public bool TryGetSubField(string subFieldKey, out IFieldWriter subFieldWriter) => TryGetField(subFieldKey, out subFieldWriter);

            public bool TryGetSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) where T : ISerializable => TryGetField<T>(subFieldKey, out subFieldWriter);

            public bool TryGetTypedFieldWriter<T>(out IFieldWriter<T> typedFieldWriter) where T : ISerializable
            {
                if (elementReference.TryGetTarget(out Element element) && element is Element<T> typedElement)
                {
                    typedFieldWriter = new GraphWriter<T>(typedElement, this.storageReference);
                    return true;
                }
                typedFieldWriter = null;
                return false;
            }

            #endregion
        }

        private class GraphWriter<T> : GraphWriter, IFieldWriter<T> where T : ISerializable
        {
            public GraphWriter(Element<T> element, GraphStorage storage) : base(element, storage) { }

            public bool TryWriteData(T data)
            {
                if (elementReference.TryGetTarget(out Element element) && element is Element<T> typedElement)
                {
                    typedElement.data = data;
                    return true;
                }
                return false;
            }
        }

        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        private List<Element> m_nodes = new List<Element>();

        protected override void AddDefaultLayers()
        {
            m_layerList.AddLayer(0, k_concrete, false);
            m_layerList.AddLayer(1, k_user,     true);
        }

        internal INodeWriter AddNodeWriterToLayer(string layerName, string id)
        {
            GraphWriter nodeWriter = AddWriterToLayer(layerName, id, out Element addedNode);
            m_nodes.Add(addedNode);
            return nodeWriter;
        }

        internal INodeReader  GetNodeReader(string id) => GetReader(id);
        internal INodeWriter  GetNodeWriterFromLayer( string layerName, string id) => GetWriterFromLayer(layerName, id);

        private GraphWriter GetWriterFromLayer(string layerName, string id)
        {
            Element element = GetElementFromLayer(layerName, id);
            if (element == null)
            {
                return null;
            }
            else
            {
                return new GraphWriter(element, this);
            }
        }
        private GraphReader GetReader(string id)
        {
            Element element = m_flatStructureLookup[id];
            if (element == null)
            {
                return null;
            }
            else
            {
                return new GraphReader(element, this);
            }
        }

        private Element GetElementFromLayer(string layerName, string id)
        {
            Element root = m_layerList.GetLayerRoot(layerName);
            if (root == null)
            {
                return null;
            }
            else
            {
                return SearchRelative(root, id);
            }
        }

        private GraphWriter AddWriterToLayer(string layerName, string id, out Element element)
        {
            element = AddElementToLayer(layerName, id);
            if(element == null)
            {
                return null;
            }
            else
            {
                return new GraphWriter(element, this);
            }
        }

        private Element AddElementToLayer(string layerName, string id)
        {
            Element root = m_layerList.GetLayerRoot(layerName);
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

        public IEnumerable<INodeReader> GetNodes()
        {
            foreach(var node in m_nodes)
            {
                yield return new GraphReader(node, this);
            }
        }


        internal void RemoveNode(string id)
        {
            Element n = SearchInternal(id);
            if(n != null)
            {
                RemoveNode(n);
            }
        }

        private void RemoveNode(Element elem)
        {
            RemoveDataBranch(elem);
        }

    }
}
