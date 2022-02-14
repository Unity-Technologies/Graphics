using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor.ContextLayeredDataStorage;
using CLDS = UnityEditor.ContextLayeredDataStorage.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IGraphDataHandler
    {
        public ElementID ID { get; }
        public T GetMetadata<T>(string lookup);
        public void SetMetadata<T>(string lookup, T data);
        public bool HasMetadata(string lookup);
    }

    public interface IEdgeHandler
    {
        public IPortHandler InputPort { get; }
        public IPortHandler OuptutPort { get; }
    }

    internal struct EdgeHandler : IEdgeHandler
    {
        public ElementID input;
        public ElementID output;
        public GraphStorage owner;

        public IPortHandler InputPort => owner.GetHandler(input);
        public IPortHandler OuptutPort => owner.GetHandler(output);
    }

    internal sealed partial class GraphStorage : CLDS
    {
        public class GraphDataHandler : IGraphDataHandler, INodeHandler, IPortHandler, IFieldHandler
        {
            public ElementID ID { get; private set; }
            private GraphStorage m_owner;

            public DataReader Reader => m_owner.Search(ID);

            public DataWriter GetWriter(string layerName)
            {
                var elem = m_owner.SearchRelative(m_owner.GetLayerRoot(layerName), ID);
                DataWriter val;
                if(elem != null)
                {
                    val = elem.GetWriter();
                }
                else
                {
                    elem = m_owner.AddElementToLayer(layerName, ID);
                    m_owner.SetHeader(elem, Reader.Element.Header); //Should we default set the header to what our reader is?
                    val = elem.GetWriter();
                }
                return val;
            }

            protected DataWriter Writer => GetWriter(k_user);
            public GraphDataHandler(ElementID elementID, GraphStorage owner)
            {
                ID      = elementID;
                m_owner = owner;
            }

            public T GetMetadata<T>(string lookup)
            {
                return Reader.Element.Header.GetMetadata<T>(lookup);
            }

            public void SetMetadata<T>(string lookup, T data)
            {
                Reader.Element.Header.SetMetadata(lookup, data);
            }

            public bool HasMetadata(string lookup)
            {
                return Reader.Element.Header.HasMetadata(lookup);
            }

            public IPortHandler AddPort(string localID, bool isInput, bool isHorizontal)
            {
                return AddPort(k_user, localID, isInput, isHorizontal);
            }

            public void ClearLayerData(string layer)
            {
                var elem = m_owner.SearchRelative(m_owner.GetLayerRoot(layer), ID);
                if (elem != null)
                {
                    m_owner.RemoveDataBranch(elem);
                }
            }

            internal GraphDataHandler AddPort(string layer, string localID, bool isInput, bool isHorizontal)
            {
                GetWriter(layer).AddChild(localID).SetHeader(new PortHeader(isInput, isHorizontal));
                return GetHandler(localID);
            }

            public IFieldHandler AddField(string localID)
            {
                return AddField(k_user, localID);
            }

            internal GraphDataHandler AddField(string layer, string localID)
            {
                GetWriter(layer).AddChild(localID).SetHeader(new FieldHeader());
                return GetHandler(localID);
            }

            public IFieldHandler<T> AddField<T>(string localID, T value)
            {
                return AddField(k_user, localID, value);
            }

            internal GraphDataHandler<T> AddField<T>(string layer, string localID, T value)
            {
                GetWriter(layer).AddChild(localID, value).SetHeader(new FieldHeader());
                return GetHandler<T>(localID);
            }
            public IFieldHandler AddSubField(string localID) => AddField(localID);
            public IFieldHandler<T> AddSubField<T>(string localID, T value) => AddField(localID, value);

            private GraphDataHandler GetHandler(string localID)
            {
                var childReader = Reader.GetChild(localID);
                if (childReader == null)
                {
                    return null;
                }
                else
                {
                    return new GraphDataHandler(childReader.Element.ID, m_owner);
                }
            }

            private GraphDataHandler<T> GetHandler<T>(string localID)
            {
                var childReader = Reader.GetChild(localID);
                if (childReader == null)
                {
                    return null;
                }
                else
                {
                    return new GraphDataHandler<T>(childReader.Element.ID, m_owner);
                }
            }

            private void RemoveHandler(string layer, string localID)
            {
                GetWriter(layer).RemoveChild(localID);
            }

            public IEnumerable<IPortHandler> GetPorts()
            {
                throw new NotImplementedException();
            }

            public IPortHandler GetPort(string localID) => GetHandler(localID);
            public IFieldHandler GetField(string localID) => GetHandler(localID);
            public IFieldHandler<T> GetField<T>(string localID) => GetHandler<T>(localID);
            public IFieldHandler GetSubField(string localID) => GetHandler(localID);
            public IFieldHandler<T> GetSubField<T>(string localID) => GetHandler<T>(localID);

            public IEnumerable<IFieldHandler> GetFields()
            {
                throw new NotImplementedException();
            }
            public IEnumerable<IFieldHandler> GetSubFields() => GetFields();

            internal void RemovePort(string layer, string localID) => RemoveHandler(layer, localID);
            public void RemovePort(string localID) => RemoveHandler(k_user, localID);
            internal void RemoveField(string layer, string localID) => RemoveHandler(layer, localID);
            public void RemoveField(string localID) => RemoveHandler(k_user, localID);
            public void RemoveSubField(string localID) => RemoveHandler(k_user, localID);

            public T GetData<T>()
            {
                return Reader.GetData<T>();
            }

            public void SetData<T>(T data)
            {
                SetData(k_user, data);
            }

            internal void SetData<T>(string layer, T data)
            {
                GetWriter(layer).SetData(data);
            }

            public bool IsInput => GetMetadata<bool>("_isInput");
            public bool IsHorizontal => GetMetadata<bool>("_isHorizontal");
        }

        public class GraphDataHandler<T> : GraphDataHandler, IFieldHandler<T>
        {
            public GraphDataHandler(ElementID elementID, GraphStorage owner) : base(elementID, owner)
            {
            }

            public T GetData()
            {
                return Reader.GetData<T>();
            }

            public void SetData(T value)
            {
                Writer.SetData(value);
            }
        }


        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        protected override void AddDefaultLayers()
        {
            AddLayer(0, k_concrete, false);
            AddLayer(1, k_user,     true);
        }

        internal GraphDataHandler GetHandler(ElementID elementID)
        {
            return new GraphDataHandler(elementID, this);
        }

        internal GraphDataHandler AddHandler(string layer, ElementID elementID, DataHeader header)
        {
            var output = new GraphDataHandler(elementID, this);
            output.GetWriter(layer).SetHeader(header);
            return output;
        }

        internal void RemoveHandler(string layer, ElementID elementID)
        {
            var elem = SearchRelative(GetLayerRoot(layer), elementID);
            if(elem != null)
            {
                RemoveDataBranch(elem);
            }
        }

        internal GraphDataHandler AddNodeHandler(string layer, ElementID elementId)
        {
            return AddHandler(layer, elementId, new NodeHeader());
        }
    }
}
