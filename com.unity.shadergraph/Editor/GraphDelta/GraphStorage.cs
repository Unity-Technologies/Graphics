using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphStorage : ContextLayeredDataStorage
    {
        private class GraphRef : IDisposable, INodeRef, IPortRef
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

            public IPortRef AddInputPort(string portID)
            {
                return storage.AddPort(this, portID, true);
            }

            public IPortRef AddOutputPort(string portID)
            {
                return storage.AddPort(this, portID, false);
            }

            public void Dispose()
            {
                element = null;
            }

            public IPortRef GetInputPort(string portID)
            {
                return storage.GetPortOnNode(this, portID, true);
            }

            public IPortRef GetOutputPort(string portID)
            {
                return storage.GetPortOnNode(this, portID, false);
            }

            public IEnumerable<IPortRef> GetInputPorts()
            {
                foreach(var port in storage.GetAllPortsOnNode(this))
                {
                    if(port.IsInput)
                    {
                        yield return port;
                    }
                }
            }

            public IEnumerable<IPortRef> GetOutputPorts()
            {
                foreach(var port in storage.GetAllPortsOnNode(this))
                {
                    if(!port.IsInput)
                    {
                        yield return port;
                    }
                }
            }

            public void Remove()
            {
                if(storage.ReferenceIsValid(this, out Element elem))
                {
                    elem.RemoveWithoutFix();
                }
            }
        }


        private List<Element> m_nodes = new List<Element>();
        public INodeRef AddNode(string id)
        {
            AddData(id, out Element elem);
            m_nodes.Add(elem);
            GraphRef output = new GraphRef(elem, this);
            return output;
        }

        public INodeRef GetNode(string id)
        {
            Element n = SearchInternal(id);
            if(n == null)
            {
                return null;
            }
            else
            {
                return new GraphRef(n, this);
            }
        }

        public IEnumerable<INodeRef> GetNodes()
        {
            foreach(var node in m_nodes)
            {
                yield return new GraphRef(node, this);
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

        private bool ReferenceIsValid(INodeRef node, out Element elem)
        {
            if(node == null)
            {
                elem = null;
                return false;
            }
            return ReferenceIsValid(node as GraphRef, out elem);
        }

        private bool ReferenceIsValid(IPortRef port, out Element elem)
        {
            if(port == null)
            {
                elem = null;
                return false;
            }
            return ReferenceIsValid(port as GraphRef, out elem);
        }

        private IPortRef AddPort(GraphRef node, string portID, bool isInput)
        {
            if (ReferenceIsValid(node, out Element elem))
            {
                Element port = elem.AddData(portID, isInput);
                return new GraphRef(port, this);
            }
            return null;
        }

        private IPortRef GetPortOnNode(GraphRef node, string portID, bool isInput)
        {
            if (ReferenceIsValid(node, out Element elem))
            {
                Element port = SearchInternal(elem.ID + "." + portID);
                if(port != null && port.TryGetData(out bool portType) && portType == isInput)
                {
                    return new GraphRef(port,this);
                }
            }
            return null;
        }

        internal bool TryConnectPorts(IPortRef a, IPortRef b)
        {
            if(a.IsInput == b.IsInput)
            {
                return false;
            }

            if(ReferenceIsValid(a, out Element portA) && ReferenceIsValid(b, out Element portB))
            {
                portA.LinkElement(portB);
                return true;
            }
            else
            {
                return false;
            }
        }

        private IEnumerable<IPortRef> GetAllPortsOnNode(GraphRef node)
        {
            if (ReferenceIsValid(node, out Element elem))
            {
                foreach (var port in elem.m_children)
                {
                    if (port.TryGetData(out bool portType))
                    {
                        yield return new GraphRef(port, this);
                    }
                }
            }
        }
    }
}
