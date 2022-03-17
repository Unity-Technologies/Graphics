using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manages the temporary edges use while creating or modifying an edge.
    /// </summary>
    public class EdgeDragHelper
    {
        internal const int panAreaWidth = 100;
        internal const int panSpeed = 4;
        internal const int panInterval = 10;
        internal const float maxSpeedFactor = 2.5f;
        internal const float maxPanSpeed = maxSpeedFactor * panSpeed;

        List<IPortModel> m_AllPorts;
        List<IPortModel> m_CompatiblePorts;
        GhostEdgeModel m_GhostEdgeModel;
        Edge m_GhostEdge;
        GraphView GraphView { get; }
        readonly EdgeConnectorListener m_Listener;
        readonly Func<IGraphModel, GhostEdgeModel> m_GhostEdgeViewModelCreator;

        IVisualElementScheduledItem m_PanSchedule;
        Vector3 m_PanDiff = Vector3.zero;
        bool m_WasPanned;

        public EdgeDragHelper(GraphView graphView, EdgeConnectorListener listener, Func<IGraphModel, GhostEdgeModel> ghostEdgeViewModelCreator)
        {
            GraphView = graphView;
            m_Listener = listener;
            m_GhostEdgeViewModelCreator = ghostEdgeViewModelCreator;
            Reset();
        }

        Edge CreateGhostEdge(IGraphModel graphModel)
        {
            GhostEdgeModel ghostEdge;

            if (m_GhostEdgeViewModelCreator != null)
            {
                ghostEdge = m_GhostEdgeViewModelCreator.Invoke(graphModel);
            }
            else
            {
                ghostEdge = new GhostEdgeModel(graphModel);
            }

            var ui = ModelViewFactory.CreateUI<Edge>(GraphView, ghostEdge);
            return ui;
        }

        GhostEdgeModel m_EdgeCandidateModel;
        Edge m_EdgeCandidate;
        public GhostEdgeModel edgeCandidateModel => m_EdgeCandidateModel;

        public void CreateEdgeCandidate(IGraphModel graphModel)
        {
            m_EdgeCandidate = CreateGhostEdge(graphModel);
            m_EdgeCandidateModel = m_EdgeCandidate.EdgeModel as GhostEdgeModel;
        }

        void ClearEdgeCandidate()
        {
            m_EdgeCandidateModel = null;
            m_EdgeCandidate = null;
        }

        public IPortModel draggedPort { get; set; }
        public Edge originalEdge { get; set; }

        public void Reset(bool didConnect = false)
        {
            if (m_AllPorts != null)
            {
                // Reset the highlights.
                for (var i = 0; i < m_AllPorts.Count; i++)
                {
                    var pv = m_AllPorts[i].GetView<Port>(GraphView);
                    if (pv != null)
                    {
                        pv.SetEnabled(true);
                        pv.Highlighted = false;
                    }
                }
                m_AllPorts = null;
            }
            m_CompatiblePorts = null;

            if (m_GhostEdge != null)
            {
                GraphView.RemoveElement(m_GhostEdge);
            }

            if (m_EdgeCandidate != null)
            {
                GraphView.RemoveElement(m_EdgeCandidate);
            }

            if (m_WasPanned)
            {
                if (didConnect)
                {
                    Vector3 p = GraphView.ContentViewContainer.transform.position;
                    Vector3 s = GraphView.ContentViewContainer.transform.scale;
                    GraphView.Dispatch(new ReframeGraphViewCommand(p, s));
                }
            }

            m_PanSchedule?.Pause();

            if (draggedPort != null && !didConnect)
            {
                var portUI = draggedPort.GetView<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                draggedPort = null;
            }

            m_GhostEdge = null;
            ClearEdgeCandidate();
        }

        public bool HandleMouseDown(MouseDownEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            if (draggedPort == null || edgeCandidateModel == null)
            {
                return false;
            }

            if (m_EdgeCandidate == null)
                return false;

            if (m_EdgeCandidate.parent == null)
            {
                GraphView.AddElement(m_EdgeCandidate);
            }

            bool startFromOutput = draggedPort.Direction == PortDirection.Output;

            edgeCandidateModel.EndPoint = mousePosition;
            m_EdgeCandidate.SetEnabled(false);

            if (startFromOutput)
            {
                edgeCandidateModel.FromPort = draggedPort;
                edgeCandidateModel.ToPort = null;
            }
            else
            {
                edgeCandidateModel.FromPort = null;
                edgeCandidateModel.ToPort = draggedPort;
            }

            var portUI = draggedPort.GetView<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = true;

            m_AllPorts = GraphView.GraphModel.GetPortModels().ToList();
            m_CompatiblePorts = GraphView.GraphModel.GetCompatiblePorts(m_AllPorts, draggedPort);

            // Only light compatible anchors when dragging an edge.
            for (var i = 0; i < m_AllPorts.Count; i++)
            {
                var pv = m_AllPorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(false);
                    pv.Highlighted = false;
                }
            }

            for (var i = 0; i < m_CompatiblePorts.Count; i++)
            {
                var pv = m_CompatiblePorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(true);
                    pv.Highlighted = true;
                }
            }

            m_EdgeCandidate.UpdateFromModel();

            if (m_PanSchedule == null)
            {
                m_PanSchedule = GraphView.schedule.Execute(Pan).Every(panInterval).StartingIn(panInterval);
                m_PanSchedule.Pause();
            }

            m_WasPanned = false;

            m_EdgeCandidate.Layer = Int32.MaxValue;

            return true;
        }

        Vector2 GetEffectivePanSpeed(Vector2 mousePos)
        {
            Vector2 effectiveSpeed = Vector2.zero;

            if (mousePos.x <= panAreaWidth)
                effectiveSpeed.x = -(((panAreaWidth - mousePos.x) / panAreaWidth) + 0.5f) * panSpeed;
            else if (mousePos.x >= GraphView.contentContainer.layout.width - panAreaWidth)
                effectiveSpeed.x = (((mousePos.x - (GraphView.contentContainer.layout.width - panAreaWidth)) / panAreaWidth) + 0.5f) * panSpeed;

            if (mousePos.y <= panAreaWidth)
                effectiveSpeed.y = -(((panAreaWidth - mousePos.y) / panAreaWidth) + 0.5f) * panSpeed;
            else if (mousePos.y >= GraphView.contentContainer.layout.height - panAreaWidth)
                effectiveSpeed.y = (((mousePos.y - (GraphView.contentContainer.layout.height - panAreaWidth)) / panAreaWidth) + 0.5f) * panSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, maxPanSpeed);

            return effectiveSpeed;
        }

        public void HandleMouseMove(MouseMoveEvent evt)
        {
            var ve = (VisualElement)evt.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(GraphView.contentContainer, evt.localMousePosition);
            m_PanDiff = GetEffectivePanSpeed(gvMousePos);

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            Vector2 mousePosition = evt.mousePosition;

            edgeCandidateModel.EndPoint = mousePosition;
            m_EdgeCandidate.UpdateFromModel();

            // Draw ghost edge if possible port exists.
            Port endPort = GetEndPort(mousePosition);

            if (endPort != null)
            {
                if (m_GhostEdge == null)
                {
                    m_GhostEdge = CreateGhostEdge(endPort.PortModel.GraphModel);
                    m_GhostEdgeModel = m_GhostEdge.EdgeModel as GhostEdgeModel;

                    m_GhostEdge.pickingMode = PickingMode.Ignore;
                    GraphView.AddElement(m_GhostEdge);
                }

                Debug.Assert(m_GhostEdgeModel != null);

                if (edgeCandidateModel.FromPort == null)
                {
                    m_GhostEdgeModel.ToPort = edgeCandidateModel.ToPort;
                    var portUI = m_GhostEdgeModel?.FromPort?.GetView<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                    m_GhostEdgeModel.FromPort = endPort.PortModel;
                    endPort.WillConnect = true;
                }
                else
                {
                    var portUI = m_GhostEdgeModel?.ToPort?.GetView<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                    m_GhostEdgeModel.ToPort = endPort.PortModel;
                    endPort.WillConnect = true;
                    m_GhostEdgeModel.FromPort = edgeCandidateModel.FromPort;
                }

                m_GhostEdge.UpdateFromModel();
            }
            else if (m_GhostEdge != null && m_GhostEdgeModel != null)
            {
                if (edgeCandidateModel.ToPort == null)
                {
                    var portUI = m_GhostEdgeModel?.ToPort?.GetView<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                }
                else
                {
                    var portUI = m_GhostEdgeModel?.FromPort?.GetView<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                }

                GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdgeModel.ToPort = null;
                m_GhostEdgeModel.FromPort = null;
                m_GhostEdgeModel = null;
                m_GhostEdge = null;
            }
        }

        void Pan()
        {
            Vector3 p = GraphView.ContentViewContainer.transform.position - m_PanDiff;
            Vector3 s = GraphView.ContentViewContainer.transform.scale;
            GraphView.UpdateViewTransform(p, s);

            edgeCandidateModel.GetView<Edge>(GraphView)?.UpdateFromModel();
            m_WasPanned = true;
        }

        public void HandleMouseUp(MouseUpEvent evt, bool isFirstEdge, IEnumerable<Edge> otherEdges, IEnumerable<IPortModel> otherPorts)
        {
            bool didConnect = false;

            Vector2 mousePosition = evt.mousePosition;

            // Reset the highlights.
            for (var i = 0; i < m_AllPorts.Count; i++)
            {
                var pv = m_AllPorts[i].GetView<Port>(GraphView);
                if (pv != null)
                {
                    pv.SetEnabled(true);
                    pv.Highlighted = false;
                }
            }

            Port portUI;
            // Clean up ghost edges.
            if (m_GhostEdgeModel != null)
            {
                portUI = m_GhostEdgeModel.ToPort?.GetView<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                portUI = m_GhostEdgeModel.FromPort?.GetView<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdgeModel.ToPort = null;
                m_GhostEdgeModel.FromPort = null;
                m_GhostEdgeModel = null;
                m_GhostEdge = null;
            }

            Port endPort = GetEndPort(mousePosition);

            if (endPort == null && m_Listener != null && isFirstEdge)
            {
                m_Listener.OnDropOutsidePort(GraphView, Enumerable.Repeat(originalEdge, 1).Concat(otherEdges), Enumerable.Repeat(draggedPort, 1).Concat(otherPorts), mousePosition, originalEdge);
            }

            m_EdgeCandidate.SetEnabled(true);

            portUI = edgeCandidateModel?.ToPort?.GetView<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = false;

            portUI = edgeCandidateModel?.FromPort?.GetView<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = false;

            // If it is an existing valid edge then delete and notify the model (using DeleteElements()).
            if (edgeCandidateModel?.ToPort == null || edgeCandidateModel?.FromPort == null)
            {
                GraphView.RemoveElement(m_EdgeCandidate);
            }

            if (endPort != null)
            {
                if (edgeCandidateModel != null)
                {
                    if (endPort.PortModel.Direction == PortDirection.Output)
                        edgeCandidateModel.FromPort = endPort.PortModel;
                    else
                        edgeCandidateModel.ToPort = endPort.PortModel;
                }

                m_Listener.OnDrop(GraphView, m_EdgeCandidate, originalEdge);
                didConnect = true;
            }
            else if (edgeCandidateModel != null)
            {
                edgeCandidateModel.FromPort = null;
                edgeCandidateModel.ToPort = null;
            }

            m_EdgeCandidate?.ResetLayer();

            ClearEdgeCandidate();
            m_CompatiblePorts = null;
            m_AllPorts = null;
            Reset(didConnect);

            originalEdge = null;
        }

        Port GetEndPort(Vector2 mousePosition)
        {
            Port endPort = null;

            foreach (var compatiblePort in m_CompatiblePorts)
            {
                var compatiblePortUI = compatiblePort.GetView<Port>(GraphView);
                if (compatiblePortUI == null || compatiblePortUI.resolvedStyle.visibility != Visibility.Visible)
                    continue;

                Rect bounds = compatiblePortUI.worldBound;
                float hitboxExtraPadding = bounds.height;

                if (compatiblePort.Orientation == PortOrientation.Horizontal)
                {
                    // Add extra padding for mouse check to the left of input port or right of output port.
                    if (compatiblePort.Direction == PortDirection.Input)
                    {
                        // Move bounds to the left by hitboxExtraPadding and increase width
                        // by hitboxExtraPadding.
                        bounds.x -= hitboxExtraPadding;
                        bounds.width += hitboxExtraPadding;
                    }
                    else if (compatiblePort.Direction == PortDirection.Output)
                    {
                        // Just add hitboxExtraPadding to the width.
                        bounds.width += hitboxExtraPadding;
                    }
                }

                // Check if mouse is over port.
                if (bounds.Contains(mousePosition))
                {
                    endPort = compatiblePortUI;
                    break;
                }
            }

            return endPort;
        }
    }
}
