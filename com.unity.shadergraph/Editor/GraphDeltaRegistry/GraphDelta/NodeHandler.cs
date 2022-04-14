using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class NodeHandler : GraphDataHandler
    {
        internal NodeHandler(ElementID elementID, GraphDelta owner, Registry registry, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, registry, defaultLayer)
        {
        }

        internal override DataHeader GetDefaultHeader()
        {
            return new NodeHeader();
        }

        [Obsolete("SetPortField is obselete - for now, use GetPort.GetTypeField().AddSubField (or we make this an extension)", false)]
        public void SetPortField<T>(string portID, string fieldID, T data) => GetPort(portID).GetTypeField().SetField(fieldID, data);

        public IEnumerable<PortHandler> GetPorts()
        {
            foreach(var child in Reader.GetChildren())
            {
                if(child.Element.Header is PortHeader)
                {
                    yield return new PortHandler(child.Element.ID, Owner, Registry, DefaultLayer);
                }
            }
        }

        public PortHandler GetPort(string localID)
        {
            return GetHandler(localID)?.ToPortHandler();
        }
        public IEnumerable<FieldHandler> GetFields()
        {
            foreach (var child in Reader.GetChildren())
            {
                if (child.Element.Header is FieldHeader)
                {
                    yield return new FieldHandler(child.Element.ID, Owner, Registry, DefaultLayer);
                }
            }
        }
        public FieldHandler GetField(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler();
        }
        public FieldHandler<T> GetField<T>(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler<T>();
        }
        public PortHandler AddPort(string localID, bool isInput, bool isHorizontal)
        {
            var w = Writer;
            var c = w.AddChild(localID);
            c.SetHeader(new PortHeader());
            var childID = ID.FullPath + $".{localID}";
            Owner.m_data.SetMetadata(childID, PortHeader.kInput, isInput);
            Owner.m_data.SetMetadata(childID, PortHeader.kHorizontal, isHorizontal);
            return new PortHandler(childID, Owner, Registry, DefaultLayer);
        }
        public void RemovePort(string localID)
        {
            Writer.RemoveChild(localID);
        }
        public FieldHandler AddField(string localID)
        {
            Writer.AddChild(localID).SetHeader(new FieldHeader());
            return new FieldHandler(ID.FullPath + $".{localID}", Owner, Registry, DefaultLayer);
        }
        public FieldHandler<T> AddField<T>(string localID, T value = default, bool reconcretizeOnDataChange = false)
        {
            Writer.AddChild(localID,value).SetHeader(new FieldHeader<T>());
            var output = new FieldHandler<T>(ID.FullPath + $".{localID}", Owner, Registry, DefaultLayer);
            if(reconcretizeOnDataChange)
            {
                output.SetMetadata(FieldHandler.k_dataRecon, true);
            }
            return output;
        }
        public void RemoveField(string localID)
        {
            Writer.RemoveChild(localID);
        }
    }
}
