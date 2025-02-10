using System;
using UnityEditor.ShaderGraph.Drawing.Interfaces;
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

        void ApplyLayoutToTargetAsStyle(Rect rect)
        {
            var resizedTarget = resizedElement.parent;
            resizedTarget.style.left = rect.x;
            resizedTarget.style.top = rect.y;
            resizedTarget.style.width = rect.width;
            resizedTarget.style.height = rect.height;
        }

        static Rect CropToParent(Rect rect, Rect parent)
        {
            rect.xMin = Mathf.Max(rect.xMin, parent.xMin);
            rect.yMin = Mathf.Max(rect.yMin, parent.yMin);
            rect.xMax = Mathf.Min(rect.xMax, parent.xMax);
            rect.yMax = Mathf.Min(rect.yMax, parent.yMax);
            return rect;
        }

        static float GetSquareSize(Rect rect, ResizableElement.Resizer resizeDirection)
        {
            var horizontal = (resizeDirection & (ResizableElement.Resizer.Left | ResizableElement.Resizer.Right)) != 0;
            var vertical = (resizeDirection & (ResizableElement.Resizer.Top | ResizableElement.Resizer.Bottom)) != 0;

            if (horizontal && vertical)
            {
                return Mathf.Min(rect.width, rect.height);
            }

            return horizontal ? rect.width : rect.height;
        }

        Rect ApplySquareAspect(Rect element, ResizableElement.Resizer resizeDirection, Rect? keepInParent = null)
        {
            var newLayout = element;
            var size = GetSquareSize(element, resizeDirection);

            size = Mathf.Clamp(size, m_MinSize.x, m_MaxSize.x);
            size = Mathf.Clamp(size, m_MinSize.y, m_MaxSize.y);
            ResizeNewLayoutAndFixPosition(resizeDirection, size);

            if (keepInParent is { } parentRect)
            {
                newLayout = CropToParent(newLayout, parentRect);
                size = Mathf.Min(newLayout.width, newLayout.height);
                ResizeNewLayoutAndFixPosition(resizeDirection, size);
            }

            return newLayout;

            void ResizeNewLayoutAndFixPosition(ResizableElement.Resizer grabbedSide, float newSize)
            {
                newLayout.width = newSize;
                newLayout.height = newSize;

                var deltaWidth = element.width - newSize;
                var deltaHeight = element.height - newSize;

                // Anchoring rules were written with the main preview in mind, which is in the bottom-right corner
                // by default.
                switch (grabbedSide)
                {
                    // Anchor to bottom-right.
                    case ResizableElement.Resizer.Top:
                    case ResizableElement.Resizer.Top | ResizableElement.Resizer.Left:
                    case ResizableElement.Resizer.Left:
                    {
                        newLayout.x = element.x + deltaWidth;
                        newLayout.y = element.y + deltaHeight;
                        break;
                    }

                    // Anchor to bottom-left.
                    case ResizableElement.Resizer.Top | ResizableElement.Resizer.Right:
                    {
                        newLayout.y = element.y + deltaHeight;
                        break;
                    }

                    // Anchor to top-left.
                    case ResizableElement.Resizer.Right:
                    case ResizableElement.Resizer.Bottom | ResizableElement.Resizer.Right:
                    case ResizableElement.Resizer.Bottom:
                    {
                        // Element is positioned by its top-left, so no adjustment is needed.
                        break;
                    }

                    // Anchor to top-right.
                    case ResizableElement.Resizer.Bottom | ResizableElement.Resizer.Left:
                    {
                        newLayout.x = element.x + deltaWidth;
                        break;
                    }

                    case ResizableElement.Resizer.None:
                    default:
                        break;
                }
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!isEnabled)
                return;

            var resizedTarget = resizedElement.parent;
            var resizedBase = resizedTarget.parent;

            // Top left position of the parent visual element
            var parentRootPosition = resizedBase.worldBound;

            // Top left of the target visual element for resizing
            var targetRootPosition = resizedTarget.worldBound;

            var sgResizable = resizedTarget as ISGResizable;
            var canResizePastParentBounds = sgResizable?.CanResizePastParentBounds() ?? false;
            var keepSquareAspect = sgResizable?.KeepSquareAspect() ?? false;

            var mousePos = resizedBase.WorldToLocal(e.mousePosition);

            if (!m_DragStarted)
            {
                sgResizable?.OnStartResize();
                m_DragStarted = true;
            }

            var newLayout = new Rect(m_StartPosition, m_StartSize);

            if ((direction & ResizableElement.Resizer.Right) != 0)
            {
                newLayout.width = m_StartSize.x + mousePos.x - m_StartMouse.x;
                var parentRightBoundary = parentRootPosition.x + resizedBase.layout.width;

                // Also ensure resizing does not happen past edge of parent views boundaries if the target does not allow it
                if (!canResizePastParentBounds)
                {
                    if ((targetRootPosition.x + newLayout.width) > parentRightBoundary)
                    {
                        var targetToRightBoundaryDelta = parentRightBoundary - targetRootPosition.x;
                        newLayout.width = targetToRightBoundaryDelta;
                    }

                    var newLayoutLeft = targetRootPosition.x - parentRootPosition.x;

                    // When resizing to right, make sure to calculate and set the target elements Style.left before resizing to ensure correct resizing behavior
                    // If Style.left is NaNpx it results in scaling towards the left
                    // This is due to how the WindowDockingLayout code affects GraphSubWindows
                    newLayout.x = newLayoutLeft;
                }

                newLayout.width = Mathf.Clamp(newLayout.width, m_MinSize.x, m_MaxSize.x);
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

                newLayout.width = -delta + m_StartSize.x;
                var targetToLeftBoundaryDelta = delta + m_StartPosition.x;

                if (!canResizePastParentBounds)
                {
                    // This ensures that the left side of the resizing target never can get pushed past the parent boundary even if mouse is moving really fast
                    targetToLeftBoundaryDelta = Mathf.Clamp(targetToLeftBoundaryDelta, 2.5f, targetToLeftBoundaryDelta);

                    // Clamps width to max out at left edge of parent window
                    if (Mathf.Approximately(targetToLeftBoundaryDelta, 2.5f))
                        newLayout.width = (m_StartPosition.x + m_StartSize.x);

                    newLayout.width = Mathf.Clamp(newLayout.width, m_MinSize.x, m_MaxSize.x);
                }

                newLayout.x = targetToLeftBoundaryDelta;
            }

            if ((direction & ResizableElement.Resizer.Bottom) != 0)
            {
                var delta = mousePos.y - m_StartMouse.y;
                newLayout.height = m_StartSize.y + delta;

                var parentBottomBoundary = parentRootPosition.y + resizedBase.layout.height;
                if (!canResizePastParentBounds)
                {
                    if ((targetRootPosition.y + newLayout.height) > parentBottomBoundary)
                    {
                        var targetToBottomBoundaryDelta = parentBottomBoundary - targetRootPosition.y;
                        newLayout.height = targetToBottomBoundaryDelta;
                    }

                    var targetToTopBoundaryDelta = targetRootPosition.y - parentRootPosition.y;

                    // When resizing to bottom, make sure to calculate and set the target elements Style.top before resizing to ensure correct resizing behavior
                    // If Style.top is NaNpx it results in scaling towards the bottom
                    // This is due to how the WindowDockingLayout code affects GraphSubWindows
                    newLayout.y = targetToTopBoundaryDelta;
                    newLayout.height = Mathf.Clamp(newLayout.height, m_MinSize.y, m_MaxSize.y);
                }
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

                newLayout.height = -delta + m_StartSize.y;
                var targetToTopBoundaryDelta = m_StartPosition.y + delta;
                if (!canResizePastParentBounds)
                {
                    // This ensures that the top of the resizing target never can get pushed past the parent boundary even if mouse is moving really fast
                    targetToTopBoundaryDelta = Mathf.Clamp(targetToTopBoundaryDelta, 2.5f, targetToTopBoundaryDelta);

                    // Clamps height to max out at top edge of parent window
                    if (Mathf.Approximately(targetToTopBoundaryDelta, 2.5f))
                        newLayout.height = (m_StartPosition.y + m_StartSize.y);

                    newLayout.height = Mathf.Clamp(newLayout.height, m_MinSize.y, m_MaxSize.y);
                }

                newLayout.y = targetToTopBoundaryDelta;
            }

            if (keepSquareAspect)
            {
                newLayout = ApplySquareAspect(
                    newLayout,
                    direction,
                    canResizePastParentBounds ? null : new Rect(0, 0, parentRootPosition.width, parentRootPosition.height)
                );
            }

            ApplyLayoutToTargetAsStyle(newLayout);
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
