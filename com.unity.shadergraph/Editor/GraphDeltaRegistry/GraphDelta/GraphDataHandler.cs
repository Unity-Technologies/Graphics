using UnityEditor.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class GraphDataHandler
    {
        public ElementID ID { get; protected set; }
        internal GraphStorage Owner { get; private set; }

        internal DataReader Reader => Owner.Search(ID);

        internal DataWriter GetWriter(string layerName)
        {
            var elem = Owner.SearchRelative(Owner.GetLayerRoot(layerName), ID);
            DataWriter val;
            if (elem != null)
            {
                val = elem.GetWriter();
            }
            else
            {
                elem = Owner.AddElementToLayer(layerName, ID);
                Owner.SetHeader(elem, Reader.Element.Header); //Should we default set the header to what our reader is?
                val = elem.GetWriter();
            }
            return val;
        }

        internal DataWriter Writer => GetWriter(GraphDelta.k_user);

        internal GraphDataHandler(ElementID elementID, GraphStorage owner)
        {
            ID = elementID;
            Owner = owner;
        }

        internal virtual T GetMetadata<T>(string lookup)
        {
            return Reader.Element.Header.GetMetadata<T>(lookup);
        }

        internal virtual void SetMetadata<T>(string lookup, T data)
        {
            Reader.Element.Header.SetMetadata(lookup, data);
        }

        internal virtual bool HasMetadata(string lookup)
        {
            return Reader.Element.Header.HasMetadata(lookup);
        }

        internal void ClearLayerData(string layer)
        {
            var elem = Owner.SearchRelative(Owner.GetLayerRoot(layer), ID);
            if (elem != null)
            {
                Owner.RemoveDataBranch(elem);
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
                return new GraphDataHandler(childReader.Element.ID, Owner);
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
    }
}
