using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing;
using UnityEngine;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class EdgeConnectorListener : IEdgeConnectorListener
    {
        readonly AbstractMaterialGraph m_Graph;

        public EdgeConnectorListener(AbstractMaterialGraph graph)
        {
            m_Graph = graph;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            var leftSlot = edge.output.userData as ISlot;
            var rightSlot = edge.input.userData as ISlot;
            if (leftSlot != null && rightSlot != null)
            {
                m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
            }
        }
    }
}
