using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        class PanManipulator : MouseManipulator
        {
            public const string k_ContentPanClassName = "content-pan";

            // Minimum distance the mouse must be dragged to be considered a drag action
            const float k_MinDragDistance = 5.0f;

            Vector2 m_PanStartPosition;
            Vector2 m_OriginalScrollOffset;
            ScrollView m_TargetScrollView;
            RenderGraphViewer m_Viewer;

            // Whether drag action is currently active
            public bool dragActive { get; private set; }

            // Whether a new drag action can be started
            public bool canStartDragging { get; set; } = true;

            public PanManipulator(RenderGraphViewer viewer)
            {
                m_Viewer = viewer;
                dragActive = false;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            }

            protected override void RegisterCallbacksOnTarget()
            {
                m_TargetScrollView = target as ScrollView;
                if (m_TargetScrollView == null)
                    throw new InvalidOperationException(
                        $"{nameof(RenderGraphViewer)}.{nameof(PanManipulator)} can only be added to a {nameof(ScrollView)}");

                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
                target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
            }

            void OnMouseDown(MouseDownEvent e)
            {
                if (dragActive)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (!canStartDragging || !CanStartManipulation(e))
                    return;

                m_PanStartPosition = e.localMousePosition;
                m_OriginalScrollOffset = m_TargetScrollView.scrollOffset;

                dragActive = true;
                target.CaptureMouse();
                target.AddToClassList(k_ContentPanClassName);

                e.StopImmediatePropagation();
            }

            Vector2 Diff(Vector2 localMousePosition)
            {
                return localMousePosition - m_PanStartPosition;
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!dragActive)
                    return;

                // Subtract the diff because we want view content to pan in the opposite direction of the movement
                m_TargetScrollView.scrollOffset = m_OriginalScrollOffset - Diff(e.localMousePosition);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!dragActive || !CanStopManipulation(e))
                    return;

                StopManipulation();
                e.StopPropagation();

                // If it was just a click, treat it as a deselect
                if (Diff(e.localMousePosition).magnitude < k_MinDragDistance)
                {
                    m_Viewer.DeselectPass();
                }
            }

            protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent evt)
            {
                if (!dragActive)
                    return;

                StopManipulation();
            }

            void StopManipulation()
            {
                dragActive = false;
                target.ReleaseMouse();
                target.RemoveFromClassList(k_ContentPanClassName);
            }
        }
    }
}
