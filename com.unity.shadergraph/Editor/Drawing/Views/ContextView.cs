using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph
{
    sealed class ContextView : StackNode
    {
        ContextData m_ContextData;

        public ContextView(ContextData contextData)
        {
            m_ContextData = contextData;

            // Header
            var headerLabel = new Label() { name = "headerLabel" };
            headerLabel.text = m_ContextData.contextName;
            headerContainer.Add(headerLabel);
        }

        public ContextData contextData => m_ContextData;
    }
}
