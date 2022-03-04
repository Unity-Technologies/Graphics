using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class NodeHandler : GraphDataHandler
    {
        internal NodeHandler(ElementID elementID, GraphStorage owner, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, defaultLayer)
        {
        }

        [Obsolete("SetPortField is obselete - for now, use GetPort.AddField", false)]
        public void SetPortField<T>(string portID, string fieldID, T data) => GetPort(portID).AddField<T>(fieldID, data);

        public IEnumerable<PortHandler> GetPorts()
        {
            foreach(var child in Reader.GetChildren())
            {
                if(child.Element.Header is PortHeader)
                {
                    yield return new PortHandler(child.Element.ID, Owner, DefaultLayer);
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
                    yield return new FieldHandler(child.Element.ID, Owner, DefaultLayer);
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
            c.SetHeader(new PortHeader(isInput, isHorizontal));
            return new PortHandler(ID.FullPath + $".{localID}", Owner, DefaultLayer);
        }
        public void RemovePort(string localID)
        {
            Writer.RemoveChild(localID);
        }
        public FieldHandler AddField(string localID)
        {
            Writer.AddChild(localID).SetHeader(new FieldHeader());
            return new FieldHandler(ID.FullPath + $".{localID}", Owner, DefaultLayer);
        }
        public FieldHandler<T> AddField<T>(string localID, T value)
        {
            Writer.AddChild(localID).SetHeader(new FieldHeader<T>());
            return new FieldHandler<T>(ID.FullPath + $".{localID}", Owner, DefaultLayer);
        }
        public void RemoveField(string localID)
        {
            Writer.RemoveChild(localID);
        }
    }
}
