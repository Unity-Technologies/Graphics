using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class FieldHandler : GraphDataHandler
    {
        internal const string k_dataRecon = "_ReconcretizeOnDataChange";
        internal FieldHandler(ElementID elementID, GraphDelta owner, Registry registry, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, registry, defaultLayer)
        {
        }

        [Obsolete("This should probably be an extension method", false)]
        public bool GetField<T>(string localID, out T data)
        {
            var nested = GetSubField<T>(localID);
            if(nested != null)
            {
                data = nested.GetData();
                return true;
            }
            data = default;
            return false;
        }

        [Obsolete("This should probably be an extension method", false)]
        internal void SetField<T>(string localID, T data)
        {
            var nested = GetSubField<T>(localID);
            if(nested != null)
            {
                nested.SetData(data);
            }
            else
            {
                AddSubField(localID, data);
            }
        }



        internal override DataHeader GetDefaultHeader()
        {
            return new FieldHeader();
        }
        public IEnumerable<FieldHandler> GetSubFields()
        {
            throw new System.NotImplementedException();
        }
        public FieldHandler GetSubField(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler();
        }
        public FieldHandler<T> GetSubField<T>(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler<T>();
        }
        public FieldHandler AddSubField(string localID)
        {
            Writer.AddChild(localID).SetHeader(new FieldHeader());
            return new FieldHandler(ID.FullPath + $".{localID}", Owner, Registry, DefaultLayer) ;
        }
        public FieldHandler<T> AddSubField<T>(string localID, T value, bool reconcretizeOnDataChange = false)
        {
            Writer.AddChild(localID, value).SetHeader(new FieldHeader<T>());
            var output = new FieldHandler<T>(ID.FullPath + $".{localID}", Owner, Registry, DefaultLayer) ;
            if(reconcretizeOnDataChange)
            {
                output.SetMetadata(k_dataRecon, true);
            }
            return output;
        }
        public void RemoveSubField(string localID)
        {
            throw new System.NotImplementedException();
        }
    }

    public class FieldHandler<T> : FieldHandler
    {
        internal FieldHandler(ElementID elementID, GraphDelta owner, Registry registry, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, registry, defaultLayer)
        {
        }

        internal override DataWriter GetWriter(string layerName)
        {
            var elem = Owner.m_data.SearchRelative(Owner.m_data.GetLayerRoot(layerName), ID);
            DataWriter val;
            if (elem != null)
            {
                val = elem.GetWriter();
            }
            else
            {
                elem = Owner.m_data.AddElementToLayer(layerName, ID, default(T));
                Owner.m_data.SetHeader(elem, GetDefaultHeader()); //Should we default set the header to what our reader is?
                val = elem.GetWriter();
            }
            return val;
        }
        internal override DataHeader GetDefaultHeader()
        {
            return new FieldHeader<T>();
        }
        public T GetData()
        {
            return Reader.GetData<T>();
        }
        public void SetData(T value)
        {
            T prev = GetData();
            Writer.SetData(value);
            if(DefaultLayer.Equals(GraphDelta.k_user) && !prev.Equals(value) && HasMetadata(k_dataRecon))
            {
                //Go up until we find an owning node, and trigger reconcretization 
                ElementID parentID = ID.ParentPath;
                while (parentID.FullPath.Length > 0)
                {
                    if (Owner.m_data.Search(parentID).Element.Header is NodeHeader)
                    {
                        Owner.ReconcretizeNode(parentID, Registry);
                        return;
                    }
                    parentID = parentID.ParentPath;
                }
            }
        }
    }
}

