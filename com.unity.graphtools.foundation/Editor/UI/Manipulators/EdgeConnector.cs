using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator used to draw an edge from one port to the other.
    /// </summary>
    public class EdgeConnector : MouseManipulator
    {
        /// <summary>
        /// The edge helper for this connector.
        /// Internally settable for tests.
        /// </summary>
        public EdgeDragHelper EdgeDragHelper { get; internal set; }

        internal const float connectionDistanceThreshold = 10f;

        bool m_Active;
        Vector2 m_MouseDownPosition;

        public EdgeConnector(GraphView graphView, Func<IGraphModel, GhostEdgeModel> ghostEdgeViewModelCreator = null)
        {
            EdgeDragHelper = new EdgeDragHelper(graphView, ghostEdgeViewModelCreator);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            var port = target.GetFirstAncestorOfType<Port>();
            if (port == null || port.PortModel.Capacity == PortCapacity.None)
            {
                return;
            }

            m_MouseDownPosition = e.localMousePosition;

            EdgeDragHelper.CreateEdgeCandidate(port.PortModel.GraphModel);
            EdgeDragHelper.draggedPort = port.PortModel;

            if (EdgeDragHelper.HandleMouseDown(e))
            {
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
            else
            {
                EdgeDragHelper.Reset();
            }
        }

        void OnCaptureOut(MouseCaptureOutEvent e)
        {
            m_Active = false;
            if (EdgeDragHelper.edgeCandidateModel != null)
                Abort();
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active) return;
            EdgeDragHelper.HandleMouseMove(e);
            e.StopPropagation();
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            try
            {
                if (CanPerformConnection(e.localMousePosition))
                    EdgeDragHelper.HandleMouseUp(e, true, Enumerable.Empty<Edge>(), Enumerable.Empty<IPortModel>());
                else
                    Abort();
            }
            finally
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            Abort();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        void Abort()
        {
            EdgeDragHelper.Reset();
        }

        bool CanPerformConnection(Vector2 mousePosition)
        {
            return Vector2.Distance(m_MouseDownPosition, mousePosition) > connectionDistanceThreshold;
        }
    }
}
