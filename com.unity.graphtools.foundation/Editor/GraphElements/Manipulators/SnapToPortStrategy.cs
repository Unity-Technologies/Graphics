using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToPortStrategy : SnapStrategy
    {
        class SnapToPortResult : SnapResult
        {
            public PortOrientation PortOrientation { get; set; }
        }
        List<Edge> m_ConnectedEdges = new List<Edge>();
        Dictionary<Port, Vector2> m_ConnectedPortsPos = new Dictionary<Port, Vector2>();

        public override void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to port already active. Call EndSnap() first.");
            }
            IsActive = true;

            if (selectedElement is Node selectedNode)
            {
                m_GraphView = selectedElement.GraphView;

                m_ConnectedEdges = GetConnectedEdges(selectedNode);
                m_ConnectedPortsPos = GetConnectedPortPositions(m_ConnectedEdges);
            }
        }

        public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.GetSnappedRect: Snap to port not active. Call BeginSnap() first.");
            }

            if (IsPaused)
            {
                // Snapping was paused, we do not return a snapped rect
                return sourceRect;
            }

            Rect snappedRect = sourceRect;

            if (selectedElement is Node selectedNode)
            {
                m_CurrentScale = scale;
                SnapToPortResult chosenResult = GetClosestSnapToPortResult(selectedNode, mousePanningDelta);

                if (chosenResult != null)
                {
                    var adjustedSourceRect = GetAdjustedSourceRect(chosenResult, sourceRect, mousePanningDelta);
                    snappedRect = adjustedSourceRect;
                    ApplySnapToPortResult(ref snappingOffset, adjustedSourceRect, ref snappedRect, chosenResult);
                }
            }

            return snappedRect;
        }

        public override void EndSnap()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.EndSnap: Snap to port already inactive. Call BeginSnap() first.");
            }
            IsActive = false;

            m_ConnectedEdges.Clear();
            m_ConnectedPortsPos.Clear();
        }

        Dictionary<Port, Vector2> GetConnectedPortPositions(List<Edge> edges)
        {
            Dictionary<Port, Vector2> connectedPortsOriginalPos = new Dictionary<Port, Vector2>();
            foreach (var edge in edges)
            {
                Port inputPort = edge.Input.GetUI<Port>(m_GraphView);
                Port outputPort = edge.Output.GetUI<Port>(m_GraphView);

                if (inputPort != null)
                {
                    if (!connectedPortsOriginalPos.ContainsKey(inputPort))
                    {
                        connectedPortsOriginalPos.Add(inputPort, inputPort.GetGlobalCenter());
                    }
                }

                if (outputPort != null)
                {
                    if (!connectedPortsOriginalPos.ContainsKey(outputPort))
                    {
                        connectedPortsOriginalPos.Add(outputPort, outputPort.GetGlobalCenter());
                    }
                }
            }

            return connectedPortsOriginalPos;
        }

        List<Edge> GetConnectedEdges(Node selectedNode)
        {
            var connectedEdges = new List<Edge>();

            for (var index = 0; index < m_GraphView.GraphModel.EdgeModels.Count; index++)
            {
                var edge = m_GraphView.GraphModel.EdgeModels[index];
                if (edge.FromPort.NodeModel == selectedNode.NodeModel || edge.ToPort.NodeModel == selectedNode.NodeModel)
                {
                    connectedEdges.Add(edge.GetUI<Edge>(m_GraphView));
                }
            }

            return connectedEdges;
        }

        SnapToPortResult GetClosestSnapToPortResult(Node selectedNode, Vector2 mousePanningDelta)
        {
            var results = GetSnapToPortResults(selectedNode);

            float smallestDraggedDistanceFromNode = float.MaxValue;
            SnapToPortResult closestResult = null;
            foreach (SnapToPortResult result in results)
            {
                // We have to consider the mouse and panning delta to estimate the distance when the node is being dragged
                float draggedDistanceFromNode = Math.Abs(result.Offset - (result.PortOrientation == PortOrientation.Horizontal ? mousePanningDelta.y : mousePanningDelta.x));
                bool isSnapping = IsSnappingToPort(draggedDistanceFromNode);

                if (isSnapping && smallestDraggedDistanceFromNode > draggedDistanceFromNode)
                {
                    smallestDraggedDistanceFromNode = draggedDistanceFromNode;
                    closestResult = result;
                }
            }

            return closestResult;
        }

        static Rect GetAdjustedSourceRect(SnapToPortResult result, Rect sourceRect, Vector2 mousePanningDelta)
        {
            Rect adjustedSourceRect = sourceRect;
            // We only want the mouse delta position and panning info on the axis that is not snapping
            if (result.PortOrientation == PortOrientation.Horizontal)
            {
                adjustedSourceRect.y += mousePanningDelta.y;
            }
            else
            {
                adjustedSourceRect.x += mousePanningDelta.x;
            }

            return adjustedSourceRect;
        }

        IEnumerable<SnapToPortResult> GetSnapToPortResults(Node selectedNode)
        {
            return m_ConnectedEdges.Select(edge => GetSnapToPortResult(edge, selectedNode)).Where(result => result != null);
        }

        SnapToPortResult GetSnapToPortResult(Edge edge, Node selectedNode)
        {
            Port sourcePort = null;
            Port snappablePort = null;

            if (edge.Output.NodeModel == selectedNode.NodeModel)
            {
                sourcePort = edge.Output.GetUI<Port>(m_GraphView);
                snappablePort = edge.Input.GetUI<Port>(m_GraphView);
            }
            else if (edge.Input.NodeModel == selectedNode.NodeModel)
            {
                sourcePort = edge.Input.GetUI<Port>(m_GraphView);
                snappablePort = edge.Output.GetUI<Port>(m_GraphView);
            }

            // We don't want to snap non existing ports and ports with different orientations (to be determined)
            if (sourcePort == null || snappablePort == null ||
                ((IPortModel)sourcePort.Model).Orientation != ((IPortModel)snappablePort.Model).Orientation)
            {
                return null;
            }

            float offset;
            if (((IPortModel)snappablePort.Model).Orientation == PortOrientation.Horizontal)
            {
                offset = m_ConnectedPortsPos[sourcePort].y - m_ConnectedPortsPos[snappablePort].y;
            }
            else
            {
                offset = m_ConnectedPortsPos[sourcePort].x - m_ConnectedPortsPos[snappablePort].x;
            }

            SnapToPortResult minResult = new SnapToPortResult
            {
                PortOrientation = ((IPortModel)snappablePort.Model).Orientation,
                Offset = offset
            };

            return minResult;
        }

        bool IsSnappingToPort(float draggedDistanceFromNode) => draggedDistanceFromNode <= SnapDistance * 1 / m_CurrentScale;

        static void ApplySnapToPortResult(ref Vector2 snappingOffset, Rect sourceRect, ref Rect r1, SnapToPortResult result)
        {
            if (result.PortOrientation == PortOrientation.Horizontal)
            {
                r1.y = sourceRect.y - result.Offset;
                snappingOffset.y = result.Offset;
            }
            else
            {
                r1.x = sourceRect.x - result.Offset;
                snappingOffset.x = result.Offset;
            }
        }
    }
}
