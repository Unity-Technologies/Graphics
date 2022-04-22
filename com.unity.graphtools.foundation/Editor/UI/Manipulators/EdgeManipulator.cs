using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator used to modify edges.
    /// </summary>
    public class EdgeManipulator : MouseManipulator
    {
        bool m_Active;
        Edge m_Edge;
        Vector2 m_PressPos;
        EdgeDragHelper m_ConnectedEdgeDragHelper;
        List<EdgeDragHelper> m_AdditionalEdgeDragHelpers;
        IPortModel m_DetachedPort;
        bool m_DetachedFromInputPort;
        static int s_StartDragDistance = 10;
        MouseDownEvent m_LastMouseDownEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeManipulator"/> class.
        /// </summary>
        public EdgeManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Reset()
        {
            m_Active = false;
            m_Edge = null;
            m_ConnectedEdgeDragHelper = null;
            m_AdditionalEdgeDragHelpers = null;
            m_DetachedPort = null;
            m_DetachedFromInputPort = false;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                StopDragging();
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            m_Edge = (evt.target as VisualElement)?.GetFirstOfType<Edge>();

            m_PressPos = evt.mousePosition;
            target.CaptureMouse();
            evt.StopPropagation();
            m_LastMouseDownEvent = evt;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            // If the left mouse button is not down then return
            if (m_Edge == null)
            {
                return;
            }

            evt.StopPropagation();

            bool alreadyDetached = (m_DetachedPort != null);

            // If one end of the edge is not already detached then
            if (!alreadyDetached)
            {
                float delta = (evt.mousePosition - m_PressPos).sqrMagnitude;

                if (delta < (s_StartDragDistance * s_StartDragDistance))
                {
                    return;
                }

                var view = m_Edge.RootView;
                var outputPortUI = m_Edge.Output.GetView<Port>(view);
                var inputPortUI = m_Edge.Input.GetView<Port>(view);

                if (outputPortUI == null || inputPortUI == null)
                {
                    return;
                }

                // Determine which end is the nearest to the mouse position then detach it.
                Vector2 outputPos = new Vector2(outputPortUI.GetGlobalCenter().x, outputPortUI.GetGlobalCenter().y);
                Vector2 inputPos = new Vector2(inputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().y);

                float distanceFromOutput = (m_PressPos - outputPos).sqrMagnitude;
                float distanceFromInput = (m_PressPos - inputPos).sqrMagnitude;

                if (distanceFromInput > 50 * 50 && distanceFromOutput > 50 * 50)
                {
                    return;
                }

                m_DetachedFromInputPort = distanceFromInput < distanceFromOutput;

                IPortModel connectedPort;
                Port connectedPortUI;


                if (m_DetachedFromInputPort)
                {
                    connectedPort = m_Edge.Output;
                    connectedPortUI = outputPortUI;

                    m_DetachedPort = m_Edge.Input;
                }
                else
                {
                    connectedPort = m_Edge.Input;
                    connectedPortUI = inputPortUI;

                    m_DetachedPort = m_Edge.Output;
                }

                // Use the edge drag helper of the still connected port
                m_ConnectedEdgeDragHelper = connectedPortUI.EdgeConnector.EdgeDragHelper;
                m_ConnectedEdgeDragHelper.originalEdge = m_Edge;
                m_ConnectedEdgeDragHelper.draggedPort = connectedPort;
                m_ConnectedEdgeDragHelper.CreateEdgeCandidate(connectedPort.GraphModel);
                m_ConnectedEdgeDragHelper.edgeCandidateModel.EndPoint = evt.mousePosition;

                // Redirect the last mouse down event to active the drag helper

                if (m_ConnectedEdgeDragHelper.HandleMouseDown(m_LastMouseDownEvent))
                {
                    m_Active = true;

                    if (m_DetachedPort.GetConnectedEdges().Count() > 1)
                    {
                        m_AdditionalEdgeDragHelpers = new List<EdgeDragHelper>();

                        foreach (var edge in m_DetachedPort.GetConnectedEdges())
                        {
                            var edgeUI = edge.GetView<Edge>(view);
                            if (edgeUI != null && edgeUI != m_Edge && edgeUI.IsSelected())
                            {
                                var otherPort = m_DetachedPort == edge.ToPort ? edge.FromPort : edge.ToPort;

                                var edgeDragHelper = otherPort.GetView<Port>(view)?.EdgeConnector.EdgeDragHelper;

                                if (edgeDragHelper != null)
                                {
                                    edgeDragHelper.originalEdge = edgeUI;
                                    edgeDragHelper.draggedPort = otherPort;
                                    edgeDragHelper.CreateEdgeCandidate(connectedPort.GraphModel);
                                    edgeDragHelper.edgeCandidateModel.EndPoint = evt.mousePosition;

                                    m_AdditionalEdgeDragHelpers.Add(edgeDragHelper);
                                }
                            }
                        }
                        foreach (var edgeDrag in m_AdditionalEdgeDragHelpers)
                        {
                            edgeDrag.HandleMouseDown(m_LastMouseDownEvent);
                        }
                    }
                }
                else
                {
                    Reset();
                }

                m_LastMouseDownEvent = null;
            }

            if (m_Active)
            {
                m_ConnectedEdgeDragHelper.HandleMouseMove(evt);
                if (m_AdditionalEdgeDragHelpers != null)
                {
                    foreach (var dragHelper in m_AdditionalEdgeDragHelpers)
                        dragHelper.HandleMouseMove(evt);
                }
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleaseMouse();
                if (m_Active)
                {
                    if (m_AdditionalEdgeDragHelpers != null)
                    {
                        m_ConnectedEdgeDragHelper.HandleMouseUp(evt, true, m_AdditionalEdgeDragHelpers.Select(t => t.originalEdge), m_AdditionalEdgeDragHelpers.Select(t => t.draggedPort));
                        foreach (var dragHelper in m_AdditionalEdgeDragHelpers)
                            dragHelper.HandleMouseUp(evt, false, Enumerable.Empty<Edge>(), Enumerable.Empty<IPortModel>());
                    }
                    else
                    {
                        m_ConnectedEdgeDragHelper.HandleMouseUp(evt, true, Enumerable.Empty<Edge>(), Enumerable.Empty<IPortModel>());
                    }
                }
                Reset();
                evt.StopPropagation();
            }
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    StopDragging();
                    evt.StopPropagation();
                }
            }
        }

        void StopDragging()
        {
            m_ConnectedEdgeDragHelper.Reset();
            if (m_AdditionalEdgeDragHelpers != null)
            {
                foreach (var dragHelper in m_AdditionalEdgeDragHelpers)
                    dragHelper.Reset();
            }

            Reset();
            target.ReleaseMouse();
        }
    }
}
