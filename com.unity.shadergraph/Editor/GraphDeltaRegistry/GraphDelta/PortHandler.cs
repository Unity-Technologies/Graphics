using System;
using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class PortHandler : GraphDataHandler
    {
        public string LocalID { get; private set; }
        public bool IsInput { get; }
        public bool IsHorizontal { get; }
        public FieldHandler GetTypeField() => GetField("TypeField");

        [Obsolete]
        public RegistryKey GetRegistryKey() { return GetTypeField().GetRegistryKey(); }

        public IEnumerable<PortHandler> GetConnectedPorts()
        {
            throw new System.Exception();
        }

        internal PortHandler(ElementID elementID, GraphStorage owner)
            : base(elementID, owner)
        {
        }

        public IEnumerable<FieldHandler> GetFields()
        {
            throw new System.Exception();
        }
        public FieldHandler GetField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> GetField<T>(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler AddField(string localID)
        {
            throw new System.Exception();
        }
        public FieldHandler<T> AddField<T>(string localID, T value)
        {
            throw new System.Exception();
        }
        public void RemoveField(string localID)
        {
            throw new System.Exception();
        }
    }
}

