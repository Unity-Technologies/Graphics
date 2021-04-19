using System;
using UnityEditor.ShaderGraph.Drawing.Interfaces;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ElementResizer : Manipulator
    {
        bool m_IsEnabled = true;
        public bool isEnabled
        {
            get => m_IsEnabled;
            set => m_IsEnabled = value;
        }

        public readonly ResizableElement.Resizer direction;

        public readonly VisualElement resizedElement;

        public ElementResizer(VisualElement resizedElement, ResizableElement.Resizer direction)
        {
            this.direction = direction;
            this.resizedElement = resizedElement;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        Vector2 m_StartMouse;
        Vector2 m_StartSize;

        Vector2 m_MinSize;
        Vector2 m_MaxSize;

        Vector2 m_StartPosition;

        bool m_DragStarted = false;

        void OnMouseDown(MouseDownEvent e)
        {
            if (!isEnabled)
                return;

            if (e.button == 0 && e.clickCount == 1)
            {
                VisualElement resizedTarget = resizedElement.parent;
                if (resizedTarget != null)
                {
                    VisualElement resizedBase = resizedTarget.parent;
                    if (resizedBase != null)
                    {
                        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                        e.StopPropagation();
                        target.CaptureMouse();
                        m_StartMouse = resizedBase.WorldToLocal(e.mousePosition);
                        m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
                        m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);

                        bool minWidthDefined = resizedTarget.resolvedStyle.minWidth != StyleKeyword.Auto;
                        bool maxWidthDefined = resizedTarget.resolvedStyle.maxWidth != StyleKeyword.None;
                        bool minHeightDefined = resizedTarget.resolvedStyle.minHeight != StyleKeyword.Auto;
                        bool maxHeightDefined = resizedTarget.resolvedStyle.maxHeight != StyleKeyword.None;
                        m_MinSize = new Vector2(
                            minWidthDefined ? resizedTarget.resolvedStyle.minWidth.value : Mathf.NegativeInfinity,
                            minHeightDefined ? resizedTarget.resolvedStyle.minHeight.value : Mathf.NegativeInfinity);
                        m_MaxSize = new Vector2(
                            maxWidthDefined ? resizedTarget.resolvedStyle.maxWidth.value : Mathf.Infinity,
                            maxHeightDefined ? resizedTarget.resolvedStyle.maxHeight.value : Mathf.Infinity);

                        m_DragStarted = false;
                    }
                }
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!isEnabled)
                return;

            VisualElement resizedTarget = resizedElement.parent;
            VisualElement resizedBase = resizedTarget.parent;

            // Top left position of the parent visual element
            var parentRootPosition = resizedBase.worldBound;
            // Top left of the target visual element for resizing
            var targetRootPosition = resizedTarget.worldBound;
            var canResizePastParentBounds = ((ISGResizable)resizedTarget).CanResizePastParentBounds();

            Vector2 mousePos = resizedBase.WorldToLocal(e.mousePosition);

            if (!m_DragStarted)
            {
                if (resizedTarget is ISGResizable resizable)
                    resizable.OnStartResize();
                m_DragStarted = true;
            }

            if ((direction & ResizableElement.Resizer.Right) != 0)
            {
                var newWidth = m_StartSize.x + mousePos.x - m_StartMouse.x;
                var parentRightBoundary = parentRootPosition.x + resizedBase.layout.width;
                // Also ensure resizing does not happen past edge of parent views boundaries if the target does not allow it
                if (!canResizePastParentBounds)
                {
                    if ((targetRootPosition.x + newWidth) > parentRightBoundary)
                    {
                        var targetToRightBoundaryDelta = parentRightBoundary - targetRootPosition.x;
                        newWidth = targetToRightBoundaryDelta;
                    }
                    var newLayoutLeft = targetRootPosition.x - parentRootPosition.x;
                    // When resizing to right, make sure to calculate and set the target elements Style.left before resizing to ensure correct resizing behavior
                    // If Style.left is NaNpx it results in scaling towards the left
                    // This is due to how the WindowDockingLayout code affects GraphSubWindows
                    resizedTarget.style.left = newLayoutLeft;
                }

                resizedTarget.style.width = Mathf.Clamp(newWidth, m_MinSize.x, m_MaxSize.x);
            }
            else if ((direction & ResizableElement.Resizer.Left) != 0)
            {
                float delta = mousePos.x - m_StartMouse.x;

                if (m_StartSize.x - delta < m_MinSize.x)
                {
                    delta = -m_MinSize.x + m_StartSize.x;
                }
                else if (m_StartSize.x - delta > m_MaxSize.x)
                {
                    delta = -m_MaxSize.x + m_StartSize.x;
                }

                var newWidth = -delta + m_StartSize.x;
                var targetToLeftBoundaryDelta = delta + m_StartPosition.x;

                if (!canResizePastParentBounds)
                {
                    // This ensures that the left side of the resizing target never can get pushed past the parent boundary even if mouse is moving really fast
                    targetToLeftBoundaryDelta = Mathf.Clamp(targetToLeftBoundaryDelta, 2.5f, targetToLeftBoundaryDelta);

                    // Clamps width to max out at left edge of parent window
                    if (Mathf.Approximately(targetToLeftBoundaryDelta, 2.5f))
                        newWidth = (m_StartPosition.x + m_StartSize.x);

                    newWidth = Mathf.Clamp(newWidth, m_MinSize.x, m_MaxSize.x);
                }

                resizedTarget.style.left = targetToLeftBoundaryDelta;
                resizedTarget.style.width = newWidth;
            }

            if ((direction & ResizableElement.Resizer.Bottom) != 0)
            {
                var delta = mousePos.y - m_StartMouse.y;
                var newHeight = m_StartSize.y + delta;

                var parentBottomBoundary = parentRootPosition.y + resizedBase.layout.height;
                if (!canResizePastParentBounds)
                {
                    if ((targetRootPosition.y + newHeight) > parentBottomBoundary)
                    {
                        var targetToBottomBoundaryDelta = parentBottomBoundary - targetRootPosition.y;
                        newHeight = targetToBottomBoundaryDelta;
                    }
                    var targetToTopBoundaryDelta = targetRootPosition.y - parentRootPosition.y;
                    // When resizing to bottom, make sure to calculate and set the target elements Style.top before resizing to ensure correct resizing behavior
                    // If Style.top is NaNpx it results in scaling towards the bottom
                    // This is due to how the WindowDockingLayout code affects GraphSubWindows
                    resizedTarget.style.top = targetToTopBoundaryDelta;

                    newHeight = Mathf.Clamp(newHeight, m_MinSize.y, m_MaxSize.y);
                }

                resizedTarget.style.height = newHeight;
            }
            else if ((direction & ResizableElement.Resizer.Top) != 0)
            {
                float delta = mousePos.y - m_StartMouse.y;

                if (m_StartSize.y - delta < m_MinSize.y)
                {
                    delta = -m_MinSize.y + m_StartSize.y;
                }
                else if (m_StartSize.y - delta > m_MaxSize.y)
                {
                    delta = -m_MaxSize.y + m_StartSize.y;
                }

                var newHeight = -delta + m_StartSize.y;
                var targetToTopBoundaryDelta = m_StartPosition.y + delta;
                if (!canResizePastParentBounds)
                {
                    // This ensures that the top of the resizing target never can get pushed past the parent boundary even if mouse is moving really fast
                    targetToTopBoundaryDelta = Mathf.Clamp(targetToTopBoundaryDelta, 2.5f, targetToTopBoundaryDelta);

                    // Clamps height to max out at top edge of parent window
                    if (Mathf.Approximately(targetToTopBoundaryDelta, 2.5f))
                        newHeight = (m_StartPosition.y + m_StartSize.y);

                    newHeight = Mathf.Clamp(newHeight, m_MinSize.y, m_MaxSize.y);
                }

                resizedTarget.style.top = targetToTopBoundaryDelta;
                resizedTarget.style.height = newHeight;
            }
            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!isEnabled)
                return;

            if (e.button == 0)
            {
                VisualElement resizedTarget = resizedElement.parent;
                if (resizedTarget.style.width != m_StartSize.x || resizedTarget.style.height != m_StartSize.y)
                {
                    if (resizedTarget is ISGResizable resizable)
                        resizable.OnResized();
                }
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}
