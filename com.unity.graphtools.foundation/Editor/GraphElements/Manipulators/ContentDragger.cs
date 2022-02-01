using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Changes the <see cref="GraphView"/> offset when the mouse is clicked and dragged in its background.
    /// </summary>
    public class ContentDragger : MouseManipulator
    {
        Vector2 m_Start;
        public Vector2 panSpeed { get; set; }

        public bool clampToParentEdges { get; set; }

        bool m_Active;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDragger"/> class.
        /// </summary>
        public ContentDragger()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            m_Start = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition);

            m_Active = true;
            target.CaptureMouse();

            graphView.ChangeMouseCursorTo((int)MouseCursor.Pan);

            e.StopImmediatePropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            Vector2 diff = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition) - m_Start;

            // During the drag update only the view
            Vector3 s = graphView.ContentViewContainer.transform.scale;
            graphView.ViewTransform.position += Vector3.Scale(diff, s);

            graphView.ChangeMouseCursorTo((int)MouseCursor.Pan);

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            Vector3 p = graphView.ContentViewContainer.transform.position;
            Vector3 s = graphView.ContentViewContainer.transform.scale;

            graphView.Dispatch(new ReframeGraphViewCommand(p, s));

            m_Active = false;
            target.ReleaseMouse();

            graphView.ChangeMouseCursorTo((int)MouseCursor.Arrow);

            e.StopPropagation();
        }
    }
}
