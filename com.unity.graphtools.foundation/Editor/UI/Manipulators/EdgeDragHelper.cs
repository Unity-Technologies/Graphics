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
        List<IPortModel> m_AllPorts;
        List<IPortModel> m_CompatiblePorts;
        GhostEdgeModel m_GhostEdgeModel;
        Edge m_GhostEdge;
        GraphView GraphView { get; }
        readonly Func<IGraphModel, GhostEdgeModel> m_GhostEdgeViewModelCreator;

        IVisualElementScheduledItem m_PanSchedule;
        Vector2 m_PanDiff = Vector2.zero;

        public EdgeDragHelper(GraphView graphView, Func<IGraphModel, GhostEdgeModel> ghostEdgeViewModelCreator)
        {
            GraphView = graphView;
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
                ghostEdge = new GhostEdgeModel { GraphModel = graphModel };
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

            if (didConnect)
            {
                var position = GraphView.ContentViewContainer.transform.position;
                var scale = GraphView.ContentViewContainer.transform.scale;
                GraphView.Dispatch(new ReframeGraphViewCommand(position, scale));
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
            var mousePosition = evt.mousePosition;

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

            var startFromOutput = draggedPort.Direction == PortDirection.Output;

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
                var panInterval = GraphView.panInterval;
                m_PanSchedule = GraphView.schedule.Execute(Pan).Every(panInterval).StartingIn(panInterval);
                m_PanSchedule.Pause();
            }

            m_EdgeCandidate.Layer = Int32.MaxValue;

            return true;
        }

        public void HandleMouseMove(MouseMoveEvent evt)
        {
            var ve = (VisualElement)evt.target;
            m_PanDiff = GraphView.GetEffectivePanSpeed(evt.mousePosition);

            if (m_PanDiff != Vector2.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            var mousePosition = evt.mousePosition;

            edgeCandidateModel.EndPoint = mousePosition;
            m_EdgeCandidate.UpdateFromModel();

            // Draw ghost edge if possible port exists.
            var endPort = GetEndPort(mousePosition);

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

        void Pan(TimerState timerState)
        {
            var travelThisFrame = m_PanDiff * timerState.deltaTime;
            var position = GraphView.ContentViewContainer.transform.position - (Vector3)travelThisFrame;
            var scale = GraphView.ContentViewContainer.transform.scale;
            GraphView.UpdateViewTransform(position, scale);

            edgeCandidateModel.GetView<Edge>(GraphView)?.UpdateFromModel();
        }

        public void HandleMouseUp(MouseUpEvent evt, bool isFirstEdge, IEnumerable<Edge> otherEdges, IEnumerable<IPortModel> otherPorts)
        {
            var didConnect = false;

            var mousePosition = evt.mousePosition;

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

            var endPort = GetEndPort(mousePosition);

            if (edgeCandidateModel != null && endPort != null)
            {
                if (endPort.PortModel.Direction == PortDirection.Output)
                    edgeCandidateModel.FromPort = endPort.PortModel;
                else
                    edgeCandidateModel.ToPort = endPort.PortModel;
            }

            // Let the first edge handle the batch command for all edges
            if (isFirstEdge)
            {
                var affectedEdges = (originalEdge != null
                        ? Enumerable.Repeat(originalEdge, 1)
                        : Enumerable.Empty<Edge>())
                    .Concat(otherEdges);

                if (endPort != null)
                {
                    if (originalEdge == null)
                        CreateNewEdge(m_EdgeCandidate.EdgeModel.FromPort, m_EdgeCandidate.EdgeModel.ToPort);
                    else
                        MoveEdges(affectedEdges, endPort);
                    didConnect = true;
                }
                else
                {
                    if (originalEdge == null)
                        DropEdgesOutside(Enumerable.Repeat(m_EdgeCandidate, 1), Enumerable.Repeat(draggedPort, 1), mousePosition);
                    else
                        DropEdgesOutside(affectedEdges, Enumerable.Repeat(draggedPort, 1).Concat(otherPorts), mousePosition);
                }
            }

            m_EdgeCandidate?.ResetLayer();

            ClearEdgeCandidate();
            m_CompatiblePorts = null;
            m_AllPorts = null;
            Reset(didConnect);

            originalEdge = null;
        }

        internal virtual void DropEdgesOutside(IEnumerable<Edge> edges,
            IEnumerable<IPortModel> portModels, Vector2 worldPosition)
        {
            if (!(GraphView.GraphModel?.Stencil is Stencil stencil))
                return;

            var localPos = GraphView.ContentViewContainer.WorldToLocal(worldPosition);

            var edgesToConnect = edges
                .Zip(portModels, (e, p) => new { edge = e, port = p })
                .Select(e => (e.edge.EdgeModel, e.port.Direction == PortDirection.Input ? EdgeSide.From : EdgeSide.To))
                .ToList();

            stencil.CreateNodesFromEdges(GraphView, GraphView.GraphTool.Preferences, GraphView.GraphModel,
                edgesToConnect, localPos, worldPosition);
        }

        internal virtual void MoveEdges(IEnumerable<Edge> edges, Port newPort)
        {
            var edgesToMove = edges.Select(e => e.EdgeModel).ToList();
            GraphView.Dispatch(new MoveEdgeCommand(newPort.PortModel, edgesToMove));
        }

        internal virtual void CreateNewEdge(IPortModel fromPort, IPortModel toPort)
        {
            GraphView.Dispatch(new CreateEdgeCommand(toPort, fromPort));
        }

        Port GetEndPort(Vector2 mousePosition)
        {
            Port endPort = null;

            foreach (var compatiblePort in m_CompatiblePorts)
            {
                var compatiblePortUI = compatiblePort.GetView<Port>(GraphView);
                if (compatiblePortUI == null || compatiblePortUI.resolvedStyle.visibility != Visibility.Visible)
                    continue;

                var bounds = compatiblePortUI.worldBound;
                var hitboxExtraPadding = bounds.height;

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
                else
                {
                    // Add extra padding for mouse check to the top of input port.
                    if (compatiblePort.Direction == PortDirection.Input)
                    {
                        // Move bounds to the top by hitboxExtraPadding and increase height
                        // by hitboxExtraPadding.
                        bounds.y -= hitboxExtraPadding;
                        bounds.height += hitboxExtraPadding;
                    }
                    else if (compatiblePort.Direction == PortDirection.Output)
                    {
                        // Just add hitboxExtraPadding to the height.
                        bounds.height += hitboxExtraPadding;
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
