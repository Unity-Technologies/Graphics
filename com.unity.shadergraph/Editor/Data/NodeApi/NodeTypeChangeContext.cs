using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public struct NodeTypeChangeContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        readonly ShaderNodeState m_State;
        readonly List<ControlDescriptor> m_CreatedControls;

        internal NodeTypeChangeContext(AbstractMaterialGraph graph, int currentSetupContextId, ShaderNodeState state, List<ControlDescriptor> createdControls)
        {
            m_Graph = graph;
            m_CurrentSetupContextId = currentSetupContextId;
            m_State = state;
            m_CreatedControls = createdControls;
        }

        public IEnumerable<NodeRef> createdNodes
        {
            get
            {
                Validate();
                // TODO: Create non-allocating version of this.
                foreach (var node in m_State.createdNodes)
                {
                    yield return new NodeRef(m_Graph, m_CurrentSetupContextId, node);
                }
            }
        }

        public IEnumerable<NodeRef> deserializedNodes
        {
            get
            {
                Validate();
                // TODO: Create non-allocating version of this.
                foreach (var node in m_State.deserializedNodes)
                {
                    yield return new NodeRef(m_Graph, m_CurrentSetupContextId, node);
                }
            }
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

        public HlslSourceRef CreateHlslSource(string source, HlslSourceType type = HlslSourceType.File)
        {
            // TODO: This file must now be watched for changes by SG
            if (type == HlslSourceType.File && !File.Exists(Path.GetFullPath(source)))
            {
                throw new ArgumentException($"Cannot open file at \"{source}\"");
            }

            m_State.hlslSources.Add(new HlslSource { source = source, type = type });
            return new HlslSourceRef(m_State.hlslSources.Count);
        }

        public void SetHlslFunction(NodeRef nodeRef, HlslFunctionDescriptor functionDescriptor)
        {
            // TODO: Validation
            // Return value must be an output port
            // All output ports must be assigned exactly once
            nodeRef.node.function = functionDescriptor;
            nodeRef.node.Dirty(ModificationScope.Graph);
        }

        // TODO: Create an overload per uniform type
        public HlslValueRef CreateHlslValue(float value)
        {
            return default;
        }

        public ControlRef CreateControl(NodeRef nodeRef, string label, float value)
        {
            m_CreatedControls.Add(new ControlDescriptor { nodeId = nodeRef.node.tempId, label = label, value = value });
            // TODO: Figure out IDs
            return default;// new ControlRef { id = 1 };
        }

        public void DestroyControl(ControlRef controlRef)
        {

        }

        public float GetControlValue(ControlRef controlRef)
        {
            return default;
        }

        public void SetControlValue(ControlRef controlRef, float value)
        {

        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeTypeChangeContext)} is only valid during the call to {nameof(IShaderNodeType)}.{nameof(IShaderNodeType.OnChange)} it was provided for.");
            }
        }
    }
}
