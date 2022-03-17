using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class PortHandler : GraphDataHandler
    {
        internal override DataHeader GetDefaultHeader()
        {
            return new PortHeader();
        }

        internal string kTypeField = "TypeField";
        public string LocalID => ID.LocalPath;
        public bool IsInput => GetMetadata<bool>(PortHeader.kInput);
        public bool IsHorizontal => GetMetadata<bool>(PortHeader.kHorizontal);
        public FieldHandler GetTypeField() => GetField(kTypeField);
        public FieldHandler AddTypeField() => AddField(kTypeField);

        [Obsolete]
        public RegistryKey GetRegistryKey() { return GetTypeField().GetRegistryKey(); }

        public IEnumerable<PortHandler> GetConnectedPorts()
        {
            bool input = GetMetadata<bool>(PortHeader.kInput);
            foreach(var edge in Owner.edges)
            {
                if(input && edge.Input.Equals(ID))
                {
                    yield return new PortHandler(edge.Output, Owner, DefaultLayer);
                }
                else if(!input && edge.Output.Equals(ID))
                {
                    yield return new PortHandler(edge.Input, Owner, DefaultLayer);
                }

            }
        }

        internal PortHandler(ElementID elementID, GraphStorage owner, string defaultLayer = GraphDelta.k_user)
            : base(elementID, owner, defaultLayer)
        {
        }

        public NodeHandler GetNode()
        {
            return new NodeHandler(ID.FullPath.Replace($".{ID.LocalPath}",""), Owner, DefaultLayer);
        }
        public IEnumerable<FieldHandler> GetFields()
        {
            throw new System.Exception();
        }
        public FieldHandler GetField(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler();
        }
        public FieldHandler<T> GetField<T>(string localID)
        {
            return GetHandler(localID)?.ToFieldHandler<T>();
        }
        public FieldHandler AddField(string localID)
        {
            Writer.AddChild(localID).SetHeader(new FieldHeader());
            return new FieldHandler(ID.FullPath + $".{localID}", Owner, DefaultLayer) ;
        }
        public FieldHandler<T> AddField<T>(string localID, T value = default)
        {
            Writer.AddChild(localID, value).SetHeader(new FieldHeader());
            return new FieldHandler<T>(ID.FullPath + $".{localID}", Owner, DefaultLayer);
        }
        public void RemoveField(string localID)
        {
            Writer.RemoveChild(localID);
        }
    }
}

