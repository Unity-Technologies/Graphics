using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class FieldHandler : GraphDataHandler
    {
        internal FieldHandler(ElementID elementID, GraphStorage owner, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, defaultLayer)
        {
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
            return new FieldHandler(ID.FullPath + $".{localID}", Owner, DefaultLayer) ;
        }
        public FieldHandler<T> AddSubField<T>(string localID, T value)
        {
            Writer.AddChild(localID, value).SetHeader(new FieldHeader<T>());
            return new FieldHandler<T>(ID.FullPath + $".{localID}", Owner, DefaultLayer) ;
        }
        public void RemoveSubField(string localID)
        {
            throw new System.Exception();
        }
    }

    public class FieldHandler<T> : FieldHandler
    {
        internal FieldHandler(ElementID elementID, GraphStorage owner, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, defaultLayer)
        {
        }

        internal override DataWriter GetWriter(string layerName)
        {
            var elem = Owner.SearchRelative(Owner.GetLayerRoot(layerName), ID);
            DataWriter val;
            if (elem != null)
            {
                val = elem.GetWriter();
            }
            else
            {
                elem = Owner.AddElementToLayer(layerName, ID, default(T));
                Owner.SetHeader(elem, Reader.Element.Header); //Should we default set the header to what our reader is?
                val = elem.GetWriter();
            }
            return val;
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
}

