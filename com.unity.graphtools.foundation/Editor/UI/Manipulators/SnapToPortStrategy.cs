using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SnapToPortStrategy : SnapStrategy
    {
        class SnapToPortResult
        {
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
            public PortOrientation PortOrientation { get; set; }

            public void Apply(ref SnapDirection snapDirection, ref Vector2 snappedRect)
            {
                if (PortOrientation == PortOrientation.Horizontal)
                {
                    snappedRect.y += Offset;
                    snapDirection |= SnapDirection.SnapY;
                }
                else
                {
                    snappedRect.x += Offset;
                    snapDirection |= SnapDirection.SnapX;
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
            base.BeginSnap(selectedElement);

            m_SelectedNodeModel = selectedElement?.Model as IPortNodeModel;
            if (m_SelectedNodeModel != null)
            {
                m_ConnectedEdges.Clear(); // should be the case already
                m_ConnectedEdges.AddRange(m_SelectedNodeModel.GetConnectedEdges());
                m_ConnectedPortsPos = GetConnectedPortPositions(selectedElement?.GraphView, m_ConnectedEdges);
            }
        }

        protected override Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            var snappedPosition = sourceRect.position;
            snapDirection = SnapDirection.SnapNone;

            if (m_SelectedNodeModel != null)
            {
                var graphView = selectedElement.GraphView;

                // snapping is used while dragging, so the element position doesn't necessarily match sourceRect
                var delta = snappedPosition - selectedElement.layout.position;
                foreach (var port in m_SelectedNodeModel.Ports
                             .Where(p => p.IsConnected())
                             .Select(p => p.GetView<Port>(graphView))
                             .Where(p => p != null))
                {
                    m_ConnectedPortsPos[port] = GetPortCenterInGraphPosition(port, graphView) + delta;
                }
                var chosenResult = GetClosestSnapToPortResult(graphView);

                chosenResult?.Apply(ref snapDirection, ref snappedPosition);
            }

            return snappedPosition;
        }

        public override void EndSnap()
        {
            base.EndSnap();

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

        SnapToPortResult GetClosestSnapToPortResult(GraphView graphView)
        {
            var results = GetSnapToPortResults(graphView);

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

        IEnumerable<SnapToPortResult> GetSnapToPortResults(GraphView graphView)
        {
            return m_ConnectedEdges.Select(e => GetSnapToPortResult(graphView, e)).Where(result => result != null);
        }

        SnapToPortResult GetSnapToPortResult(GraphView graphView, IEdgeModel edge)
        {
            var fromPort = edge.FromPort.GetView<Port>(graphView);
            var toPort = edge.ToPort.GetView<Port>(graphView);

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
