using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public struct NodeChangeContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_Id;
        readonly NodeTypeState m_TypeState;
        readonly List<ControlRef> m_CreatedControls;

        internal NodeChangeContext(AbstractMaterialGraph graph, int id, NodeTypeState typeState, List<ControlRef> createdControls) : this()
        {
            m_Graph = graph;
            m_Id = id;
            m_TypeState = typeState;
            m_CreatedControls = createdControls;
        }

        internal AbstractMaterialGraph graph => m_Graph;

        internal int id => m_Id;

        internal NodeTypeState typeState => m_TypeState;

        public object GetData(NodeRef nodeRef)
        {
            Validate();

            return nodeRef.node.data;
        }

        // TODO: Decide whether this should be immediate
        // The issue could be that an exception is thrown mid-way, and then the node is left in a halfway broken state.
        // Maybe we can verify that it's valid immediately, but then postpone setting the value until the whole method
        // finishes.
        public void SetData(NodeRef nodeRef, object value)
        {
            Validate();

            var type = value.GetType();
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException($"{type.FullName} cannot be used as node data because it doesn't have a public, parameterless constructor.");
            }

            // TODO: Maybe do a proper check for whether the type is serializable?
            nodeRef.node.data = value;
        }

        public void SetHlslFunction(NodeRef nodeRef, HlslFunctionDescriptor functionDescriptor)
        {
            Validate();
            // TODO: Validation
            // Return value must be an output port
            // All output ports must be assigned exactly once
            // TODO: Copy input
            nodeRef.node.function = functionDescriptor;
            nodeRef.node.Dirty(ModificationScope.Graph);
        }

        // TODO: Create an overload per uniform type
        public HlslValueRef CreateHlslValue(float value)
        {
            Validate();
            var hlslValueRef = new HlslValueRef(m_TypeState.hlslValues.Count);
            m_TypeState.hlslValues.Add(new HlslValue { value = value });
            return hlslValueRef;
        }

        // TODO: Create an overload per uniform type
        public void SetHlslValue(HlslValueRef hlslValueRef, float value)
        {
            Validate();
            // TODO: Validate
            // TODO: Different dirtying strategy
            m_Graph.shouldRepaintPreviews = true;
            var hlslValue = m_TypeState.hlslValues[hlslValueRef.index];
            hlslValue.value = value;
            m_TypeState.hlslValues[hlslValueRef.index] = hlslValue;
        }

        public ControlRef CreateControl(NodeRef nodeRef, string label, float value)
        {
            Validate();

            // TODO: Clean up when a node is deleted
            var controlDescriptor = new ControlState { nodeId = nodeRef.node.tempId, label = label, value = value };
            var controlRef = new ControlRef(typeState.controls.Count);
            typeState.controls.Add(controlDescriptor);
            m_CreatedControls.Add(controlRef);
            return controlRef;
        }

        public void DestroyControl(ControlRef controlRef)
        {
            Validate();

            throw new NotImplementedException();
        }

        public bool WasControlModified(ControlRef controlRef)
        {
            Validate();
            return typeState.controls[controlRef.index].wasModified;
        }

        public float GetControlValue(ControlRef controlRef)
        {
            Validate();
            return typeState.controls[controlRef.index].value;
        }

        public void SetControlValue(ControlRef controlRef, float value)
        {
            Validate();
            throw new NotImplementedException();
        }

        internal void Validate()
        {
            if (m_Id != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeChangeContext)} is only valid during the {nameof(ShaderNodeType)} it was provided for.");
            }
        }
    }
}
