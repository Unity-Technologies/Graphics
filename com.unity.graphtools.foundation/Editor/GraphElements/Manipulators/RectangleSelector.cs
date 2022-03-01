using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator to select elements by drawing a rectangle around them.
    /// </summary>
    public class RectangleSelector : MouseManipulator
    {
        readonly RectangleSelect m_Rectangle;
        bool m_Active;

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleSelector"/> class.
        /// </summary>
        public RectangleSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
            m_Rectangle = new RectangleSelect();
            m_Rectangle.style.position = Position.Absolute;
            m_Rectangle.style.top = 0f;
            m_Rectangle.style.left = 0f;
            m_Rectangle.style.bottom = 0f;
            m_Rectangle.style.right = 0f;
            m_Active = false;
        }

        // get the axis aligned bound
        Rect ComputeAxisAlignedBound(Rect position, Matrix4x4 transform)
        {
            Vector3 min = transform.MultiplyPoint3x4(position.min);
            Vector3 max = transform.MultiplyPoint3x4(position.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                m_Rectangle.RemoveFromHierarchy();
                m_Active = false;
            }
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (CanStartManipulation(e))
            {
                if (!e.actionKey)
                {
                    graphView.Dispatch(new ClearSelectionCommand());
                }

                graphView.Add(m_Rectangle);

                m_Rectangle.start = e.localMousePosition;
                m_Rectangle.end = m_Rectangle.start;

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
            }
        }

        static readonly List<ModelUI> k_OnMouseUpAllUIs = new List<ModelUI>();
        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (!CanStopManipulation(e))
                return;

            graphView.Remove(m_Rectangle);

            m_Rectangle.end = e.localMousePosition;

            var selectionRect = new Rect()
            {
                min = new Vector2(Math.Min(m_Rectangle.start.x, m_Rectangle.end.x), Math.Min(m_Rectangle.start.y, m_Rectangle.end.y)),
                max = new Vector2(Math.Max(m_Rectangle.start.x, m_Rectangle.end.x), Math.Max(m_Rectangle.start.y, m_Rectangle.end.y))
            };

            selectionRect = ComputeAxisAlignedBound(selectionRect, graphView.ViewTransform.matrix.inverse);

            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ModelUI> newSelection = new List<ModelUI>();
            graphView.GraphModel?.GraphElementModels
                .Where(ge => ge.IsSelectable())
                .GetAllUIsInList(graphView, null, k_OnMouseUpAllUIs);
            foreach (var child in k_OnMouseUpAllUIs)
            {
                var localSelRect = graphView.ContentViewContainer.ChangeCoordinatesTo(child, selectionRect);
                if (child.Overlaps(localSelRect))
                {
                    newSelection.Add(child);
                }
            }
            k_OnMouseUpAllUIs.Clear();

            var mode = e.actionKey ? SelectElementsCommand.SelectionMode.Toggle : SelectElementsCommand.SelectionMode.Add;
            var newSelectedModels = newSelection.Select(elem => elem.Model).ToList();
            graphView.Dispatch(new SelectElementsCommand(mode, newSelectedModels));

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_Rectangle.end = e.localMousePosition;
            e.StopPropagation();
        }

        class RectangleSelect : ImmediateModeElement
        {
            public Vector2 start { get; set; }
            public Vector2 end { get; set; }

            protected override void ImmediateRepaint()
            {
                VisualElement t = parent;

                // Avoid drawing useless information
                if (start == end)
                    return;

                var r = new Rect
                {
                    min = new Vector2(Math.Min(start.x, end.x), Math.Min(start.y, end.y)),
                    max = new Vector2(Math.Max(start.x, end.x), Math.Max(start.y, end.y))
                };

                var lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
                var segmentSize = 5f;

                Vector3[] points =
                {
                    new Vector3(r.xMin, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMax, 0.0f),
                    new Vector3(r.xMin, r.yMax, 0.0f)
                };

                DrawDottedLine(points[0], points[1], segmentSize, lineColor);
                DrawDottedLine(points[1], points[2], segmentSize, lineColor);
                DrawDottedLine(points[2], points[3], segmentSize, lineColor);
                DrawDottedLine(points[3], points[0], segmentSize, lineColor);
            }

            void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
            {
                GraphViewStaticBridge.ApplyWireMaterial();

                GL.Begin(GL.LINES);
                GL.Color(col);

                float length = Vector3.Distance(p1, p2); // ignore z component
                int count = Mathf.CeilToInt(length / segmentsLength);
                for (int i = 0; i < count; i += 2)
                {
                    GL.Vertex((Vector3.Lerp(p1, p2, i * segmentsLength / length)));
                    GL.Vertex((Vector3.Lerp(p1, p2, (i + 1) * segmentsLength / length)));
                }

                GL.End();
            }
        }
    }
}
