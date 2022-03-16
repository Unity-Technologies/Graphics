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
            public override bool Equals(object obj)
            {
                if(obj is GraphReader other)
                {
                    if(elementReference.TryGetTarget(out var myElem) && other.elementReference.TryGetTarget(out var otherElem))
                    {
                        return myElem == otherElem;
                    }
                }
                return false;
            }

            public override int GetHashCode()
            {
                if(elementReference.TryGetTarget(out var elem))
                {
                    return storageReference.GetHashCode() + elem.GetHashCode();
                }
                else
                {
                    return base.GetHashCode();
                }
            }

            private bool IsPortReader(Element element)
            {
                return HasSubData(element, "_isInput", "_isHorizontal");
            }

            private string path;
            public WeakReference<Element> elementReference;
            public GraphStorage storageReference;
            public void Dispose()
            {
                elementReference = null;
                storageReference = null;
            }

            public GraphReader(Element element, GraphStorage storage)
            {
                this.path = element.GetFullPath();
                this.elementReference = new WeakReference<Element>(storage.m_flatStructureLookup[path]);
                this.storageReference = storage;
            }

            private IEnumerable<Element> GetSubElements()
            {
                foreach(var kvp in storageReference.m_flatStructureLookup)
                {
                    string id = kvp.Key;
                    Element elem = kvp.Value;
                    if (id.StartsWith(path))
                    {
                        var relativePath = id.Substring(path.Length);
                        if(relativePath.StartsWith("."))
                        {
                            yield return elem;
                        }
                    }
                }

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
                if (elementReference.TryGetTarget(out Element element) && element is Element<T> typeElement)
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

            private bool TryGetSubReader(string searchKey, out GraphReader graphReader, bool thruConnection = true)
            {
                if(thruConnection && storageReference.m_flatStructureLookup.TryGetValue($"{path}._Input", out Element element) && element is Element<string> connection)
                {
                    if(storageReference.m_flatStructureLookup.TryGetValue($"{connection.data}.{searchKey}", out Element connected))
                    {
                        graphReader = new GraphReader(connected, this.storageReference);
                        return true;
                    }
                }
                if (storageReference.m_flatStructureLookup.TryGetValue($"{path}.{searchKey}", out element))
                {
                    graphReader = new GraphReader(element, this.storageReference);
                    return true;
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

            public string GetFullPath()
            {
                elementReference.TryGetTarget(out var element);
                return element.GetFullPath();
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
                if (TryGetSubReader(portKey, out GraphReader graphReader))
                {
                    if (graphReader.elementReference.TryGetTarget(out Element element) && IsPortReader(element))
                    {
                        portReader = graphReader;
                        return true;
                    }
                }
                portReader = null;
                return false;
            }

            bool INodeReader.TryGetField(string fieldKey, out IFieldReader fieldReader)
            {
                if (TryGetSubReader(fieldKey, out GraphReader graphReader))
                {
                    if (graphReader.elementReference.TryGetTarget(out Element element) && !IsPortReader(element))
                    {
                        fieldReader = graphReader;
                        return true;
                    }
                }
                fieldReader = null;
                return false;
            }

            public bool TryGetField(string fieldKey, out IFieldReader fieldReader, bool thruConnection = true)
            {
                bool output = TryGetSubReader(fieldKey, out GraphReader reader, thruConnection);
                fieldReader = reader;
                return output;
            }
            public bool TryGetSubField(string fieldKey, out IFieldReader fieldReader) => TryGetField(fieldKey, out fieldReader);

            public IEnumerable<IPortReader> GetConnectedPorts()
            {
                if(IsInput())
                {
                    if (TryGetField("_Input", out var fieldReader))
                    {
                        fieldReader.TryGetValue<string>(out var elementReferenceName);
                        storageReference.m_flatStructureLookup.TryGetValue(elementReferenceName, out Element elementReference);
                        yield return new GraphReader(elementReference, storageReference);
                    }
                }
                else
                {
                    if (TryGetField("_Output", out var fieldReader))
                    {
                        fieldReader.TryGetValue<List<string>>(out var elementReferenceNames);
                        foreach (var name in elementReferenceNames)
                        {
                            storageReference.m_flatStructureLookup.TryGetValue(name, out Element elementReference);
                            yield return new GraphReader(elementReference, storageReference);
                        }
                    }
                }
            }

            public IEnumerable<IFieldReader> GetSubFields() => GetFields();

            public bool IsInput()
            {
                if(TryGetField("_isInput", out var reader, false) && reader.TryGetValue(out bool isInput))
                {
                    return isInput;
                }
                return false;
            }

            public bool IsHorizontal()
            {
                if (TryGetField("_isHorizontal", out var reader, false) && reader.TryGetValue(out bool isInput))
                {
                    return isInput;
                }
                return false;
            }

            public INodeReader GetNode()
            {
                string parentPath = path.Substring(0, path.LastIndexOf('.'));
                if(storageReference.m_flatStructureLookup.TryGetValue(parentPath, out Element elem))
                {
                    return new GraphReader(elem, storageReference);
                }
                return null;
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

            private bool TryAddSubWriter<T>(string key, out GraphWriter<T> graphWriter)
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
                    var connection = storageReference.SearchRelative(element, "_Input");
                    if(connection != null && connection is Element<Element> link)
                    {
                        element = link.data;
                    }
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

            private bool TryGetSubWriter<T>(string key, out GraphWriter<T> graphWriter)
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    var connection = storageReference.SearchRelative(element, "_Input");
                    if(connection != null && connection is Element<string> link)
                    {
                        storageReference.m_flatStructureLookup.TryGetValue(link.data, out element);
                    }
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
                    if (thisReader.IsInput())
                    {
                        ConnectPorts(otherWriter, this);
                    }
                    else
                    {
                        ConnectPorts(this, otherWriter);
                    }
                    return true;
                }
                return false;
            }

            private void ConnectPorts(GraphWriter output, GraphWriter input)
            {
                input.elementReference.TryGetTarget(out Element inputElem);
                output.elementReference.TryGetTarget(out Element outputElem);

                if(!output.TryGetSubWriter("_Output", out GraphWriter<List<string>> outputWriter))
                {
                    output.TryAddSubWriter("_Output", out outputWriter);
                    outputWriter.TryWriteData(new List<string>());
                }
                outputWriter.elementReference.TryGetTarget(out Element listElem);
                (listElem as Element<List<string>>).data.Add(inputElem.GetFullPath());

                if(!input.TryGetSubWriter("_Input", out GraphWriter<string> inputWriter))
                {
                    input.TryAddSubWriter("_Input", out inputWriter);
                }
                inputWriter.TryWriteData(outputElem.GetFullPath());
            }

            private void DisconnectPorts(GraphWriter output, GraphWriter input)
            {
                input.elementReference.TryGetTarget(out Element inputElem);
                output.elementReference.TryGetTarget(out Element outputElem);

                output.TryGetSubWriter("_Output", out GraphWriter<List<string>> outputWriter);

                outputWriter.elementReference.TryGetTarget(out Element elem);
                var listElem = (elem as Element<List<string>>);
                listElem.data.Remove(inputElem.GetFullPath());
                if(listElem.data.Count == 0)
                {
                    storageReference.RemoveData(elem);
                }

                input.TryGetSubWriter("_Input", out GraphWriter<string> inputWriter);
                inputWriter.elementReference.TryGetTarget(out Element elem2);
                storageReference.RemoveData(elem2);
            }

            public bool TryAddField<T>(string fieldKey, out IFieldWriter<T> fieldWriter)
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

            public bool TryAddSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) => TryAddField<T>(subFieldKey, out subFieldWriter);

            public bool TryAddTransientNode<T>(string transientNodeKey, IFieldWriter<T> fieldWriter)
            {
                throw new NotImplementedException();
            }

            public bool TryGetField<T>(string fieldKey, out IFieldWriter<T> fieldWriter)
            {
                bool output = TryGetSubWriter(fieldKey, out GraphWriter<T> subWriter);
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetField(string fieldKey, out IFieldWriter fieldWriter)
            {
                bool output = TryGetSubWriter(fieldKey, out var subWriter);
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetPort(string portKey, out IPortWriter portWriter)
            {
                bool output = TryGetSubWriter(portKey, out var subWriter);
                portWriter = subWriter;
                return output;
            }

            public bool TryGetSubField(string subFieldKey, out IFieldWriter subFieldWriter) => TryGetField(subFieldKey, out subFieldWriter);

            public bool TryGetSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) => TryGetField<T>(subFieldKey, out subFieldWriter);

            public bool TryGetTypedFieldWriter<T>(out IFieldWriter<T> typedFieldWriter)
            {
                if (elementReference.TryGetTarget(out Element element) && element is Element<T> typedElement)
                {
                    typedFieldWriter = new GraphWriter<T>(typedElement, this.storageReference);
                    return true;
                }
                typedFieldWriter = null;
                return false;
            }

            bool INodeWriter.TryRemove()
            {
                if(elementReference.TryGetTarget(out Element element))
                {
                    bool output = true;
                    foreach(var child in element.children)
                    {
                        if (HasSubData(child, "_isInput", "_isHorizontal"))
                        {
                            DisconnectAll(child);
                        }
                    }
                    storageReference.RemoveDataBranch(element);
                    return output;
                }
                return false;
            }

            private void DisconnectAll(Element element)
            {
                foreach (var child in element.children)
                {
                    if (child.id.CompareTo("_Input") == 0)
                    {
                        var outputPort = (child as Element<Element>).data;
                        DisconnectPorts(new GraphWriter(outputPort, storageReference), this);
                        break;
                    }
                    else if (child.id.CompareTo("_Output") == 0)
                    {
                        var connections = (child as Element<List<Element>>).data;
                        foreach (var connection in connections)
                        {
                            DisconnectPorts(this, new GraphWriter(connection, storageReference));
                        }
                        break;
                    }
                }
            }

            bool IPortWriter.TryRemove()
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    bool output = true;
                    foreach (var child in element.children)
                    {
                        if(child.id.CompareTo("_Input") == 0)
                        {
                            var outputPort = (child as Element<Element>).data;
                            DisconnectPorts(new GraphWriter(outputPort, storageReference), this);
                            break;
                        }
                        else if (child.id.CompareTo("_Output") == 0)
                        {
                            var connections = (child as Element<List<Element>>).data;
                            foreach(var connection in connections)
                            {
                                DisconnectPorts(this, new GraphWriter(connection, storageReference));
                            }
                            break;
                        }
                    }
                    storageReference.RemoveDataBranch(element);
                    return output;
                }
                return false;

            }

            bool IFieldWriter.TryRemove()
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    bool output = true;
                    storageReference.RemoveDataBranch(element);
                    return output;
                }
                return false;

            }

            public IPortWriter GetPort(string portKey)
            {
                if(!TryGetPort(portKey, out var pw))
                {
                    var reader = GetCorrespondingReader();
                    if(reader.TryGetPort(portKey, out var portReader))
                    {
                        TryAddPort(portKey, portReader.IsInput(), portReader.IsHorizontal(), out pw);
                    }
                }
                return pw;
            }




            #endregion
        }

        private class GraphWriter<T> : GraphWriter, IFieldWriter<T>
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

        private static bool HasSubData(Element element, params string[] expectedChildren)
        {
            foreach (string childName in expectedChildren)
            {
                var check = GetSubData(element, childName);
                if (check == null)
                {
                    return false;
                }
            }
            return true;
        }

        private static Element GetSubData(Element element, string dataID)
        {
            foreach (var child in element.children)
            {
                if (child.id.CompareTo(dataID) == 0)
                {
                    return child;
                }
            }
            return null;
        }

        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        protected override void AddDefaultLayers()
        {
            m_layerList.AddLayer(0, k_concrete, false);
            m_layerList.AddLayer(1, k_user,     true);
        }

        internal INodeWriter AddNodeWriterToLayer(string layerName, string id)
        {
            GraphWriter nodeWriter = AddWriterToLayer(layerName, id, out Element addedNode);
            return nodeWriter;
        }

        internal IEnumerable<INodeReader> GetAllChildReaders()
        {
            foreach (var data in m_flatStructureLookup.Values)
            {
                // For now, we'll just... assume that all immediate children of the graph are nodes.
                // Ouch.
                var reader = new GraphReader(data, this);
                yield return reader;
            }
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
            if (m_flatStructureLookup.TryGetValue(id, out var element))
            {
                return new GraphReader(element, this);
            }

            return null;
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
