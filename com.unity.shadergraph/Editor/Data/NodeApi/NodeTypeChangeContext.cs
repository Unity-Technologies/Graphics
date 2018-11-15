using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public struct NodeTypeChangeContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_Id;
        readonly NodeTypeState m_TypeState;
        readonly List<ControlDescriptor> m_CreatedControls;

        internal NodeTypeChangeContext(AbstractMaterialGraph graph, int id, NodeTypeState typeState, List<ControlDescriptor> createdControls)
        {
            m_Graph = graph;
            m_Id = id;
            m_TypeState = typeState;
            m_CreatedControls = createdControls;
        }

        internal AbstractMaterialGraph graph => m_Graph;

        internal int id => m_Id;

        internal NodeTypeState typeState => m_TypeState;

        public NodeRefEnumerable createdNodes => new NodeRefEnumerable(m_Graph, m_Id, m_TypeState.createdNodes);

        public NodeRefEnumerable deserializedNodes => new NodeRefEnumerable(m_Graph, m_Id, m_TypeState.deserializedNodes);

        public NodeRefEnumerable modifiedNodes => new NodeRefEnumerable(m_Graph, m_Id, m_TypeState.modifiedNodes);

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
            if (type == HlslSourceType.File && !File.Exists(Path.GetFullPath(source)))
            {
                throw new ArgumentException($"Cannot open file at \"{source}\"");
            }

            m_TypeState.hlslSources.Add(new HlslSource { source = source, type = type });
            return new HlslSourceRef(m_TypeState.hlslSources.Count);
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

        public bool WasControlModified(ControlRef controlRef)
        {
            return false;
        }

        public float GetControlValue(ControlRef controlRef)
        {
            return default;
        }

        public void SetControlValue(ControlRef controlRef, float value)
        {

        }

        internal void Validate()
        {
            if (m_Id != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeTypeChangeContext)} is only valid during the call to {nameof(IShaderNodeType)}.{nameof(IShaderNodeType.OnChange)} it was provided for.");
            }
        }
    }
}
