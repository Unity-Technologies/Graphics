
using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    public abstract class BaseShaderGraphConstant : IConstant //, ISerializationCallbackReceiver
    {
        [SerializeField]
        private object tempSerializedValue;

        [SerializeReference]
        protected ShaderGraphModel graphModel;

        GraphHandler graphHandler => graphModel.GraphHandler;

        [SerializeField]
        protected string nodeName, portName;
        public bool IsInitialized => !string.IsNullOrEmpty(nodeName) && graphHandler != null;
        public FieldHandler GetField()
        {
            if (!IsInitialized) return null;
            var nodeReader = graphHandler.GetNode(nodeName);
            var portReader = nodeReader.GetPort(portName);
            return portReader.GetTypeField();
        }
        public string NodeName => nodeName;
        public string PortName => portName;
        public void Initialize(ShaderGraphModel graphModel, string nodeName, string portName)
        {
            if (!IsInitialized)
            {
                this.graphModel = graphModel;
                this.nodeName = nodeName;
                this.portName = portName;
            }
        }
        public object ObjectValue {
            get => IsInitialized ? GetValue() : DefaultValue;
            set {
                if (IsInitialized)
                    SetValue(value);
            }
        }

        abstract protected object GetValue();
        abstract protected void SetValue(object value);
        abstract public object DefaultValue { get; }
        abstract public Type Type { get; }
        abstract public TypeHandle GetTypeHandle();

        public void Initialize(TypeHandle constantTypeHandle) { }
        public IConstant Clone() { return null; }

        //public void OnBeforeSerialize() => tempSerializedValue = ObjectValue;
        //public void OnAfterDeserialize() => ObjectValue = tempSerializedValue;
    }
}
