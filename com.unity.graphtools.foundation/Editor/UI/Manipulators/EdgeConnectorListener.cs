using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Handles the end of edge drawing.
    /// </summary>
    public class EdgeConnectorListener
    {
        Action<GraphView, IEnumerable<Edge>, IEnumerable<IPortModel>, Vector2> m_OnDropOutsideDelegate;
        Action<GraphView, Edge> m_OnDropDelegate;

        public void SetDropOutsideDelegate(Action<GraphView, IEnumerable<Edge>, IEnumerable<IPortModel>, Vector2> action)
        {
            m_OnDropOutsideDelegate = action;
        }

        public void SetDropDelegate(Action<GraphView, Edge> action)
        {
            m_OnDropDelegate = action;
        }

        public void OnDropOutsidePort(GraphView graphView, IEnumerable<Edge> edges, IEnumerable<IPortModel> ports, Vector2 position, Edge originalEdge)
        {
            if (m_OnDropOutsideDelegate != null)
            {
                m_OnDropOutsideDelegate(graphView, edges, ports, position);
            }
            else
            {
                graphView.Dispatch(new DeleteEdgeCommand(edges.Where(e => !(e is IGhostEdge)).Select(e => e.EdgeModel).ToList()));
            }
        }

        public void OnDrop(GraphView graphView, Edge edge, Edge originalEdge)
        {
            if (m_OnDropDelegate != null)
            {
                m_OnDropDelegate(graphView, edge);
            }
            else
            {

                if (edge.EdgeModel?.ToPort?.IsConnectedTo(edge.EdgeModel.FromPort) ?? false)
                    return;

                if (edge.EdgeModel?.ToPort == null || edge.EdgeModel.FromPort == null)
                {
                    graphView.Dispatch(new DeleteEdgeCommand(new List<IEdgeModel> { edge.EdgeModel }));
                }
                else
                {
                    // TODO VladN: The original edge should be moved not deleted and recreated.
                    // We can then safely remove EdgesToDelete parameter from the Command
                    // should be addressed by GTF-723
                    graphView.Dispatch(new CreateEdgeCommand(
                        edge.EdgeModel.ToPort,
                        edge.EdgeModel.FromPort,
                        originalEdge != null ? new []{ originalEdge.EdgeModel } : null
                    ));
                }
            }
        }
    }
}
