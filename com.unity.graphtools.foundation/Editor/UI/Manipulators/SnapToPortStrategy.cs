using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToPortStrategy : SnapStrategy
    {
        class SnapToPortResult : SnapResult
        {
            public PortOrientation PortOrientation { get; set; }

            public void Apply(ref Vector2 snappingOffset, ref Rect snappedRect)
            {
                if (PortOrientation == PortOrientation.Horizontal)
                {
                    snappedRect.y += Offset;
                    snappingOffset.y = Offset;
                }
                else
                {
                    snappedRect.x += Offset;
                    snappingOffset.x = Offset;
                }
            }
        }

        /// <summary>
        /// Model of the node we try to snap.
        /// </summary>
         IPortNodeModel m_SelectedNodeModel;

        /// <summary>
        /// List of edges connected to the node to snap.
        /// </summary>
        List<IEdgeModel> m_ConnectedEdges = new List<IEdgeModel>();

        /// <summary>
        /// Position in the graph of every potential port to snap to.
        /// </summary>
        Dictionary<Port, Vector2> m_ConnectedPortsPos = new Dictionary<Port, Vector2>();

        public override void BeginSnap(GraphElement selectedElement)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.BeginSnap: Snap to port already active. Call EndSnap() first.");
            }
            IsActive = true;

            m_SelectedNodeModel = selectedElement?.Model as IPortNodeModel;
            if (m_SelectedNodeModel != null)
            {
                m_GraphView = selectedElement.GraphView;

                m_ConnectedEdges.Clear(); // should be the case already
                m_ConnectedEdges.AddRange(m_SelectedNodeModel.GetConnectedEdges());
                m_ConnectedPortsPos = GetConnectedPortPositions(m_GraphView, m_ConnectedEdges);
            }
        }

        public override Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement)
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

            if (m_SelectedNodeModel != null)
            {
                // snapping is used while dragging, so the element position doesn't necessarily match sourceRect
                var delta = sourceRect.position - selectedElement.layout.position;
                foreach (var port in m_SelectedNodeModel.Ports
                             .Where(p => p.IsConnected())
                             .Select(p => p.GetView<Port>(m_GraphView))
                             .Where(p => p != null))
                {
                    m_ConnectedPortsPos[port] = GetPortCenterInGraphPosition(port, m_GraphView) + delta;
                }
                var chosenResult = GetClosestSnapToPortResult();

                chosenResult?.Apply(ref snappingOffset, ref sourceRect);
            }

            return sourceRect;
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

        static Dictionary<Port, Vector2> GetConnectedPortPositions(GraphView graphView, List<IEdgeModel> edges)
        {
            Dictionary<Port, Vector2> connectedPortsOriginalPos = new Dictionary<Port, Vector2>();
            foreach (var edge in edges)
            {
                Port inputPort = edge.ToPort.GetView<Port>(graphView);
                if (inputPort != null && !connectedPortsOriginalPos.ContainsKey(inputPort))
                {
                    connectedPortsOriginalPos.Add(inputPort, GetPortCenterInGraphPosition(inputPort, graphView));
                }

                Port outputPort = edge.FromPort.GetView<Port>(graphView);
                if (outputPort != null && !connectedPortsOriginalPos.ContainsKey(outputPort))
                {
                    connectedPortsOriginalPos.Add(outputPort, GetPortCenterInGraphPosition(outputPort, graphView));
                }
            }

            return connectedPortsOriginalPos;
        }

        static Vector2 GetPortCenterInGraphPosition(Port port, GraphView graphView)
        {
            var gvPos = new Vector2(graphView.ViewTransform.position.x, graphView.ViewTransform.position.y);
            var gvScale = graphView.ViewTransform.scale.x;

            var connector = port.GetConnector();
            var localCenter = connector.layout.size * .5f;
            return connector.ChangeCoordinatesTo(graphView.contentContainer, localCenter - gvPos) / gvScale;
        }

        SnapToPortResult GetClosestSnapToPortResult()
        {
            var results = GetSnapToPortResults();

            float smallestDraggedDistanceFromNode = float.MaxValue;
            SnapToPortResult closestResult = null;
            foreach (var result in results)
            {
                var distanceFromPortToSnap = result.Distance;
                var isSnapping = IsSnappingToPort(distanceFromPortToSnap);

                if (isSnapping && smallestDraggedDistanceFromNode > distanceFromPortToSnap)
                {
                    smallestDraggedDistanceFromNode = distanceFromPortToSnap;
                    closestResult = result;
                }
            }

            return closestResult;
        }

        IEnumerable<SnapToPortResult> GetSnapToPortResults()
        {
            return m_ConnectedEdges.Select(GetSnapToPortResult).Where(result => result != null);
        }

        SnapToPortResult GetSnapToPortResult(IEdgeModel edge)
        {
            var fromPort = edge.FromPort.GetView<Port>(m_GraphView);
            var toPort = edge.ToPort.GetView<Port>(m_GraphView);

            // We don't want to snap non existing ports and ports with different orientations (to be determined)
            if (fromPort == null || toPort == null || edge.FromPort.Orientation !=edge.ToPort.Orientation)
            {
                return null;
            }

            var orientation = edge.FromPort.Orientation;
            var sourcePort = m_SelectedNodeModel == edge.FromPort.NodeModel ? fromPort : toPort;
            var targetPort = m_SelectedNodeModel == edge.FromPort.NodeModel ? toPort : fromPort;

            var portDelta = m_ConnectedPortsPos[targetPort] - m_ConnectedPortsPos[sourcePort];

            return new SnapToPortResult
            {
                PortOrientation = orientation,
                Offset = orientation == PortOrientation.Horizontal ? portDelta.y : portDelta.x
            };
        }

        bool IsSnappingToPort(float distanceFromSnapPoint) => distanceFromSnapPoint <= SnapDistance;
    }
}
