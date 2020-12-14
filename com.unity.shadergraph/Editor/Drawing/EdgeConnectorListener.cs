using UnityEngine;
using UnityEditor.Experimental.GraphView;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using UnityEditor.Searcher;

namespace UnityEditor.ShaderGraph.Drawing
{
    class EdgeConnectorListener : IEdgeConnectorListener
    {
        readonly GraphData m_Graph;
        readonly SearchWindowProvider m_SearchWindowProvider;
        readonly EditorWindow m_editorWindow;

        public EdgeConnectorListener(GraphData graph, SearchWindowProvider searchWindowProvider, EditorWindow editorWindow)
        {
            m_Graph = graph;
            m_SearchWindowProvider = searchWindowProvider;
            m_editorWindow = editorWindow;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            var draggedPort = (edge.output != null ? edge.output.edgeConnector.edgeDragHelper.draggedPort : null) ?? (edge.input != null ? edge.input.edgeConnector.edgeDragHelper.draggedPort : null);
            m_SearchWindowProvider.connectedPort = (ShaderPort)draggedPort;
            m_SearchWindowProvider.regenerateEntries = true;//need to be sure the entires are relevant to the edge we are dragging
            SearcherWindow.Show(m_editorWindow, (m_SearchWindowProvider as SearcherProvider).LoadSearchWindow(),
                item => (m_SearchWindowProvider as SearcherProvider).OnSearcherSelectEntry(item, position),
                position, null);
            m_SearchWindowProvider.regenerateEntries = true;//entries no longer necessarily relevant, need to regenerate
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            var leftSlot = edge.output.GetSlot();
            var rightSlot = edge.input.GetSlot();
            if (leftSlot != null && rightSlot != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
            }
        }
    }
}
