
using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // [Serializable]
    public abstract class ICLDSConstant : IConstant //, ISerializationCallbackReceiver
    {
        // [SerializeField]
        // private object tempSerializedValue;

        protected GraphHandler graphHandler;
        protected string nodeName, portName;
        public bool IsInitialized => nodeName != null && nodeName != "" && graphHandler != null;

        public FieldHandler GetField()
        {
            if (!IsInitialized) return null;
            var nodeReader = graphHandler.GetNode(nodeName);
            var portReader = nodeReader.GetPort(portName);
            return portReader.GetTypeField();
        }

        public string NodeName => nodeName;
        public string PortName => portName;


        public void Initialize(GraphHandler handler, string nodeName, string portName)
        {
            if (!IsInitialized)
            {
                this.graphHandler = handler;
                this.nodeName = nodeName;
                this.portName = portName;
            }
        }


        abstract protected object GetValue();
        abstract protected void SetValue(object value);



        public object ObjectValue {
            get => IsInitialized ? GetValue() : DefaultValue;
            set {
                if (IsInitialized)
                    SetValue(value);
            }
        }

        abstract public object DefaultValue { get; }
        abstract public Type Type { get; }
        public void Initialize(TypeHandle constantTypeHandle) { }
        public IConstant Clone() { return null; }
        abstract public TypeHandle GetTypeHandle();

        // public void OnBeforeSerialize() => tempSerializedValue = ObjectValue;
        // public void OnAfterDeserialize() => ObjectValue = tempSerializedValue;
    }
}
