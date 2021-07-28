using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphStorage : ContextLayeredDataStorage
    {
        private class GraphReader : IDisposable, INodeReader, IPortReader, IFieldReader
        {

            private struct PortFlagsStruct 
            {
                public bool isInput;
                public bool isHorizontal;
            }

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
                    return element.m_children;
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
                    Element maybeElement = storageReference.SearchRecurse(element, searchKey);
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
                    Element maybePort = storageReference.SearchRecurse(element, portKey);
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
                    Element maybeField = storageReference.SearchRecurse(element, fieldKey);
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

            #endregion

        }

        private class GraphRef
        {
            public WeakReference<Element> element;
            public GraphStorage storage;

            public GraphRef(Element element, GraphStorage storage)
            {
                this.element = new WeakReference<Element>(element);
                this.storage = storage;
            }

            public bool IsInput
            {
                get
                {
                    if(element.TryGetTarget(out Element elem) && elem.TryGetData(out bool isInput))
                    {
                        return isInput;
                    }
                    return false;
                }
            }

            /*
            public GraphRef AddInputPort(string portID)
            {
                return storage.AddPort(this, portID, true);
            }

            public GraphRef AddOutputPort(string portID)
            {
                return storage.AddPort(this, portID, false);
            }

            public void Dispose()
            {
                element = null;
            }

            public IPortReader GetInputPort(string portID)
            {
                return storage.GetPortOnNode(this, portID, true);
            }

            public GraphRef GetOutputPort(string portID)
            {
                return storage.GetPortOnNode(this, portID, false);
            }

            public IEnumerable<GraphRef> GetInputPorts()
            {
                foreach(var port in storage.GetAllPortsOnNode(this))
                {
                    if(port.IsInput)
                    {
                        yield return port;
                    }
                }
            }

            public IEnumerable<GraphRef> GetOutputPorts()
            {
                foreach(var port in storage.GetAllPortsOnNode(this))
                {
                    if(!port.IsInput)
                    {
                        yield return port;
                    }
                }
            }
*/
            public void Remove()
            {
                if(storage.ReferenceIsValid(this, out Element elem))
                {
                    elem.RemoveWithoutFix();
                }
            }
        }


        private List<Element> m_nodes = new List<Element>();
        /*
        public INodeRef AddNode(string id)
        {
            AddData(id, out Element elem);
            m_nodes.Add(elem);
            GraphRef output = new GraphRef(elem, this);
            return output;
        }

        */
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

        private bool ReferenceIsValid(GraphRef graphRef, out Element elem)
        {
            elem = null;
            if(graphRef != null && graphRef.element.TryGetTarget(out elem))
            {
                return true;
            }
            return false;
        }

        private bool ReferenceIsValid(INodeReader node, out Element elem)
        {
            if(node == null)
            {
                elem = null;
                return false;
            }
            return ReferenceIsValid(node as GraphRef, out elem);
        }

        private bool ReferenceIsValid(IPortReader port, out Element elem)
        {
            if(port == null)
            {
                elem = null;
                return false;
            }
            return ReferenceIsValid(port as GraphRef, out elem);
        }

        /*
        private IPortRef AddPort(GraphRef node, string portID, bool isInput)
        {
            if (ReferenceIsValid(node, out Element elem))
            {
                Element port = elem.AddData(portID, isInput);
                return new GraphRef(port, this);
            }
            return null;
        }
        */

    }
}
