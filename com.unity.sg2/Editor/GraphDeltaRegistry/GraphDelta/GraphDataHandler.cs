using System;
using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class ChildNotFoundException : Exception
    {
        public ChildNotFoundException(GraphDataHandler handler, string localID)
            : base($"Expected a child element with ID {localID} at {handler.ID.FullPath}, but no child was found")
        { }
        
    }

    public class DataTypeMismatch : InvalidCastException
    {
        public DataTypeMismatch(GraphDataHandler handler, Type T)
            : base($"Expected element data at {handler.ID.FullPath} to be of type {T.AssemblyQualifiedName}, but was {handler.Reader.Element.GetType().GetGenericTypeDefinition().AssemblyQualifiedName}") { }
    }

    public class GraphDataHandler
    {
        public ElementID ID { get; protected set; }
        internal GraphDelta Owner { get; private set; }
        internal Registry Registry { get; private set; }
        internal string DefaultLayer { get; set; }
        internal DataReader Reader => Owner.m_data.Search(ID);

        [Obsolete("GetName is Obsolete - use ID.LocalPath instead", false)]
        public string GetName() => ID.LocalPath;

        internal virtual DataWriter GetWriter(string layerName)
        {
            var elem = Owner.m_data.SearchRelative(Owner.m_data.GetLayerRoot(layerName), ID);
            DataWriter val;
            if (elem != null)
            {
                val = elem.GetWriter();
            }
            else
            {
                elem = Owner.m_data.AddElementToLayer(layerName, ID);
                Owner.m_data.SetHeader(elem, GetDefaultHeader());
                val = elem.GetWriter();
            }
            return val;
        }

        internal virtual DataHeader GetDefaultHeader()
        {
            return new DataHeader();
        }

        internal DataWriter Writer => GetWriter(DefaultLayer);

        internal GraphDataHandler(ElementID elementID, GraphDelta owner, Registry registry, string defaultLayer = GraphDelta.k_user)
        {
            ID = elementID;
            Owner = owner;
            Registry = registry;
            DefaultLayer = defaultLayer;
        }

        internal virtual T GetMetadata<T>(string lookup)
        {
            return Owner.m_data.GetMetadata<T>(ID, lookup);
        }

        internal virtual void SetMetadata<T>(string lookup, T data)
        {
            Owner.m_data.SetMetadata(ID, lookup, data);
        }

        internal virtual bool HasMetadata(string lookup)
        {
            return Owner.m_data.HasMetadata(ID, lookup);
        }

        internal void ClearLayerData(string layer)
        {
            var elem = Owner.m_data.SearchRelative(Owner.m_data.GetLayerRoot(layer), ID);
            if (elem != null)
            {
                Owner.m_data.RemoveDataBranch(elem);
            }
        }

        protected GraphDataHandler GetHandler(string localID)
        {
            var childReader = Reader.GetChild(localID);
            if (childReader == null)
            {
                return null;
            }
            else
            {
                return new GraphDataHandler(childReader.Element.ID, Owner, Registry, DefaultLayer);
            }
        }

        protected void RemoveHandler(string layer, string localID)
        {
            GetWriter(layer).RemoveChild(localID);
        }

        public T GetData<T>()
        {
            return Reader.GetData<T>();
        }

        public void SetData<T>(T data)
        {
            SetData(GraphDelta.k_user, data);
        }

        internal void SetData<T>(string layer, T data)
        {
            GetWriter(layer).SetData(data);
        }

        public NodeHandler ToNodeHandler()
        {
            if(Reader.Element.Header is NodeHeader)
            {
                return new NodeHandler(ID, Owner, Registry, DefaultLayer);
            }
            return null;
        }

        public PortHandler ToPortHandler()
        {
            if (Reader.Element.Header is PortHeader)
            {
                return new PortHandler(ID, Owner, Registry, DefaultLayer);
            }
            return null;
        }

        public FieldHandler ToFieldHandler()
        {
            if (Reader.Element.Header is FieldHeader)
            {
                return new FieldHandler(ID, Owner, Registry, DefaultLayer);
            }
            return null;
        }

        public FieldHandler<T> ToFieldHandler<T>()
        {
            if (Reader.Element.Header is FieldHeader<T>)
            {
                return new FieldHandler<T>(ID, Owner, Registry, DefaultLayer);
            }
            return null;
        }


    }
}
