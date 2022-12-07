
using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    abstract class BaseShaderGraphConstant : Constant, ISerializationCallbackReceiver
    {
        [SerializeReference]
        protected SGGraphModel graphModel;
        [SerializeField]
        protected string nodeName;
        [SerializeField]
        protected string portName;
        GraphHandler graphHandler => graphModel.GraphHandler;

        // TODO: shouldn't need to special case if we're a searcher preview.
        NodeHandler nodeHandler => graphHandler?.GetNode(nodeName)
            ?? graphModel.RegistryInstance.DefaultTopologies.GetNode(nodeName);

        public bool IsInitialized => !string.IsNullOrEmpty(nodeName) && graphHandler != null && nodeHandler != null;
        public FieldHandler GetField()
        {
            if (!IsInitialized) return null;
            try
            {
                var portReader = nodeHandler.GetPort(portName);
                var typeField = portReader.GetTypeField();
                return typeField;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
        public string NodeName => nodeName;
        public string PortName => portName;
        public void Initialize(SGGraphModel graphModel, string nodeName, string portName)
        {
            if (!IsInitialized)
            {
                this.graphModel = graphModel;
                this.nodeName = nodeName;
                this.portName = portName;
            }

            var storedValue = GetStoredValueForCopy();
            // If when initializing this port we find that the value type has changed, refresh stored value
            if(storedValue?.GetType() != ObjectValue?.GetType())
                StoreValueForCopy();
        }

        public override object ObjectValue
        {
            get => IsInitialized ? GetValue() : DefaultValue;
            set {
                if (IsInitialized)
                {
                    OwnerModel?.GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(OwnerModel, ChangeHint.Data);
                    SetValue(value);
                }
            }
        }

        // TODO: Do this in CLDS instead
        abstract protected void StoreValueForCopy();
        abstract public object GetStoredValueForCopy();

        abstract protected object GetValue();
        abstract protected void SetValue(object value);

        public override void Initialize(TypeHandle constantTypeHandle) { }

        public override Constant Clone()
        {
            var copy = (BaseShaderGraphConstant)Activator.CreateInstance(GetType());
            copy.Initialize(graphModel, nodeName, portName);
            var storedValue = GetStoredValueForCopy();
            copy.ObjectValue = storedValue;
            return copy;
        }

        bool HasBackingVariableBeenDeleted()
        {
            if (graphModel is null) return false;

            foreach (var model in graphModel.VariableDeclarations)
            {
                // If we can find a variable that is tied to this constant, we're fine!
                if (model is GraphDataVariableDeclarationModel variableDeclarationModel
                && variableDeclarationModel.graphDataName == portName)
                {
                    return false;
                }
            }

            // If we get to here, this constant has been orphaned and someone's maintaining a leaky reference
            return true;
        }

        public void OnBeforeSerialize()
        {
            // TODO: (Sai) this is a memory leak of some sort
            // TODO: the owning variable gets deleted so someone else is maintaining a reference
            if (nodeName == graphModel?.BlackboardContextName && HasBackingVariableBeenDeleted())
                return;
            StoreValueForCopy();
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
