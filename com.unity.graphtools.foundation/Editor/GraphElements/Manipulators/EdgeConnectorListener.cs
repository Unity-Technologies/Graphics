using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
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

        public static List<IEdgeModel> GetDropEdgeModelsToDelete(IEdgeModel edge)
        {
            List<IEdgeModel> edgeModelsToDelete = new List<IEdgeModel>();

            if (edge.ToPort != null && edge.ToPort.Capacity == PortCapacity.Single)
            {
                foreach (var edgeToDelete in edge.ToPort.GetConnectedEdges())
                {
                    if (!ReferenceEquals(edgeToDelete, edge) && !(edgeToDelete is GhostEdgeModel))
                        edgeModelsToDelete.Add(edgeToDelete);
                }
            }

            if (edge.FromPort != null && edge.FromPort.Capacity == PortCapacity.Single)
            {
                foreach (var edgeToDelete in edge.FromPort.GetConnectedEdges())
                {
                    if (!ReferenceEquals(edgeToDelete, edge) && !(edgeToDelete is GhostEdgeModel))
                        edgeModelsToDelete.Add(edgeToDelete);
                }
            }

            return edgeModelsToDelete;
        }

        public void OnDropOutsidePort(GraphView graphView, IEnumerable<Edge> edges, IEnumerable<IPortModel> ports, Vector2 position, Edge originalEdge)
        {
            if (m_OnDropOutsideDelegate != null)
            {
                m_OnDropOutsideDelegate(graphView, edges, ports, position);
            }
            else
            {
                List<IEdgeModel> edgesToDelete = new List<IEdgeModel>();
                foreach (var edge in edges)
                {
                    edgesToDelete.AddRange(GetDropEdgeModelsToDelete(edge.EdgeModel));
                    // when grabbing an existing edge's end, the edgeModel should be deleted
                    if (!(edge.EdgeModel is GhostEdgeModel))
                        edgesToDelete.Add(edge.EdgeModel);

                    if (originalEdge != null)
                        edgesToDelete.Add(originalEdge.EdgeModel);
                }

                graphView.Dispatch(new DeleteEdgeCommand(edgesToDelete));
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
                List<IEdgeModel> edgeModelsToDelete = GetDropEdgeModelsToDelete(edge.EdgeModel);

                if (edge.EdgeModel?.ToPort?.IsConnectedTo(edge.EdgeModel.FromPort) ?? false)
                    return;

                // when grabbing an existing edge's end, the edgeModel should be deleted
                if (!(edge.EdgeModel is GhostEdgeModel))
                    edgeModelsToDelete.Add(edge.EdgeModel);

                if (originalEdge != null)
                    edgeModelsToDelete.Add(originalEdge.EdgeModel);

                if (edge.EdgeModel?.ToPort == null || edge.EdgeModel.FromPort == null)
                {
                    graphView.Dispatch(new DeleteEdgeCommand(new List<IEdgeModel> { edge.EdgeModel }));
                }
                else
                {
                    graphView.Dispatch(new CreateEdgeCommand(
                        edge.EdgeModel.ToPort,
                        edge.EdgeModel.FromPort,
                        edgeModelsToDelete
                    ));
                }
            }
        }
    }
}
