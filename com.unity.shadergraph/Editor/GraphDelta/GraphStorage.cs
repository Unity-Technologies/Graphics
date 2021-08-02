using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public struct PortFlagsStruct : ISerializable
    {
        public bool isInput;
        public bool isHorizontal;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("isInput", isInput, typeof(bool));
            info.AddValue("isHorizontal", isHorizontal, typeof(bool));
        }

        public PortFlagsStruct(SerializationInfo info, StreamingContext context)
        {
            isInput = info.GetBoolean("isInput");
            isHorizontal = info.GetBoolean("isHorizontal");
        }
    }

    internal sealed class GraphStorage : ContextLayeredDataStorage.ContextLayeredDataStorage
    {
        private class GraphReader : IDisposable, INodeReader, IPortReader, IFieldReader, IDataReader
        {
            private bool IsPortReader(Element element)
            {
                if(element.TryGetData(out PortFlagsStruct _))
                {
                    return true;
                }
                return false;
            }

            public WeakReference<Element> elementReference;
            public GraphStorage storageReference;
            public void Dispose()
            {
                elementReference = null;
                storageReference = null;
            }

            public GraphReader(Element element, GraphStorage storage)
            {
                this.elementReference = new WeakReference<Element>(element);
                this.storageReference = storage;
            }

            private IEnumerable<Element> GetSubElements()
            {
                if(elementReference.TryGetTarget(out Element element))
                {
                    return element.children;
                }
                return null;
            }

            public IEnumerator<GraphReader> GetChildren()
            {
                foreach(var subElement in GetSubElements())
                {
                    yield return new GraphReader(subElement, this.storageReference);
                }
            }

            public bool TryGetValue<T>(out T value)
            {
                if(elementReference.TryGetTarget(out Element element) && element is Element<T> typeElement)
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
                if (elementReference.TryGetTarget(out Element element))
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
                foreach(var subElement in GetSubElements())
                {
                    if(IsPortReader(subElement))
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            public IEnumerable<IPortReader> GetInputPorts()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (subElement.TryGetData(out PortFlagsStruct value) && value.isInput)
                    {
                        yield return new GraphReader(subElement, this.storageReference);
                    }
                }
            }

            public IEnumerable<IPortReader> GetOutputPorts()
            {
                foreach (var subElement in GetSubElements())
                {
                    if (subElement.TryGetData(out PortFlagsStruct value) && !value.isInput)
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
                if(elementReference.TryGetTarget(out Element element))
                {
                    Element maybePort = storageReference.SearchRelative(element, portKey);
                    if(IsPortReader(maybePort))
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
                if (elementReference.TryGetTarget(out Element element))
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

            public PortFlagsStruct GetFlags()
            {
                TryGetValue<PortFlagsStruct>(out var value);
                return value;
            }

            public IEnumerable<IPortReader> GetConnectedPorts()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IFieldReader> GetSubFields() => GetFields();

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


            private bool TryAddSubWriter(string key, out GraphWriter graphWriter)
            {
                if (elementReference.TryGetTarget(out Element element))
                {
                    Element addedData = element.AddData(key);
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
                    Element<T> addedData = element.AddData(key, default(T));
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
                    if(subElement != null)
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
                    if(subElement != null && subElement is Element<T> typedSubElement)
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
                if(other != null
                && elementReference.TryGetTarget(out Element element) && element is Element<PortFlagsStruct> portElement
                && otherWriter.elementReference.TryGetTarget(out Element otherElement) && otherElement is Element<PortFlagsStruct> otherPortElement)
                {
                    if (portElement.data.isInput != otherPortElement.data.isInput && portElement.data.isHorizontal == otherPortElement.data.isHorizontal)
                    {
                        element.LinkElement(otherElement);
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
                var output = TryAddSubWriter(portKey, out GraphWriter<PortFlagsStruct> graphWriter);
                graphWriter.TryWriteData(new PortFlagsStruct() { isInput = isInput, isHorizontal = isHorizontal });
                portWriter = graphWriter;
                return output;
            }

            public bool TryAddSubField(string subFieldKey, out IFieldWriter subFieldWriter) => TryAddField(subFieldKey, out subFieldWriter);

            public bool TryAddSubField<T>(string subFieldKey, out IFieldWriter<T> subFieldWriter) where T : ISerializable => TryAddField<T>(subFieldKey, out subFieldWriter);

            public bool TryAddTransientNode<T>(string transientNodeKey, IFieldWriter<T> fieldWriter) where T : ISerializable
            {
                throw new NotImplementedException();
            }

            public bool TryGetField<T>(string fieldKey, out IFieldWriter<T> fieldWriter) where T : ISerializable
            {
                var output = TryGetSubWriter(fieldKey, out GraphWriter<T> subWriter);
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetField(string fieldKey, out IFieldWriter fieldWriter)
            {
                var output = TryGetSubWriter(fieldKey, out GraphWriter subWriter);
                fieldWriter = subWriter;
                return output;
            }

            public bool TryGetPort(string portKey, out IPortWriter portWriter)
            {
                var output = TryGetSubWriter(portKey, out GraphWriter<PortFlagsStruct> subWriter);
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
                if(elementReference.TryGetTarget(out Element element) && element is Element<T> typedElement)
                {
                    typedElement.data = data;
                    return true;
                }
                return false;
            }
        }


        private List<Element> m_nodes = new List<Element>();

        public INodeWriter AddNode(string id)
        {
            AddData(id, out Element elem);
            m_nodes.Add(elem);
            GraphWriter output = new GraphWriter(elem, this);
            return output;
        }


        public INodeReader GetNode(string id)
        {
            Element n = SearchInternal(id);
            if(n == null)
            {
                return null;
            }
            else
            {
                return new GraphReader(n, this);
            }
        }

        public INodeWriter GetNodeWriter(string id)
        {
            Element n = SearchInternal(id);
            if (n == null)
            {
                return null;
            }
            else
            {
                return new GraphWriter(n, this);
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
            //Everything with this node should also be removed, so all owned element children
            //(ports, edges, settings, etc) can just be GC'd
            elem.RemoveWithoutFix();
        }

    }
}
