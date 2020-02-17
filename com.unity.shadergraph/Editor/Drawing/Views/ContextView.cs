using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    sealed class ContextView : StackNode
    {
        ContextData m_ContextData;

        // Currently we only need one Port per context
        // As the Contexts are hardcoded we know their directions
        Port m_Port;

        // When dealing with more Contexts, `name` should be serialized in the ContextData
        // Right now we dont do this so we dont overcommit to serializing unknowns
        public ContextView(string name, ContextData contextData)
        {
            m_ContextData = contextData;

            // Header
            var headerLabel = new Label() { name = "headerLabel" };
            headerLabel.text = name;
            headerContainer.Add(headerLabel);
        }

        public ContextData contextData => m_ContextData;
        public Port port => m_Port;

        public void AddPort(Direction direction)
        {
            var capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
            var container = direction == Direction.Input ? inputContainer : outputContainer;
            m_Port = Port.Create<Edge>(Orientation.Vertical, direction, capacity, null);
            m_Port.portName = "";

            // Vertical ports have no representation in Model
            // Therefore we need to disable interaction
            m_Port.pickingMode = PickingMode.Ignore;

            container.Add(m_Port);
        }
    }
}
