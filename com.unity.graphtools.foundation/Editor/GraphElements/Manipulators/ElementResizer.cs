using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator to handle interactions to resize an element.
    /// </summary>
    class ElementResizer : MouseManipulator
    {
        readonly ResizerDirection direction;

        readonly VisualElement resizedElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementResizer"/> class.
        /// </summary>
        /// <param name="resizedElement">The resized element.</param>
        /// <param name="direction">The directions in which the element can be resized.</param>
        public ElementResizer(VisualElement resizedElement, ResizerDirection direction)
        {
            this.direction = direction;
            this.resizedElement = resizedElement;
            activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        Vector2 m_StartMouse;
        Vector2 m_StartSize;
        Vector2 m_StartPosition;

        Vector2 m_MinSize;
        Vector2 m_MaxSize;

        Rect m_NewRect;

        bool m_DragStarted;

        bool m_Active;

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            m_Active = true;

            VisualElement resizedTarget = resizedElement.parent;
            if (resizedTarget == null)
                return;
            VisualElement resizedBase = resizedTarget.parent;
            if (resizedBase == null)
                return;
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            e.StopPropagation();
            target.CaptureMouse();
            m_StartMouse = resizedBase.WorldToLocal(e.mousePosition);
            m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
            m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);
            m_NewRect = new Rect(m_StartPosition, m_StartSize);

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

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            VisualElement resizedTarget = resizedElement.parent;
            VisualElement resizedBase = resizedTarget.parent;

            Vector2 mousePos = resizedBase.WorldToLocal(e.mousePosition);
            if (!m_DragStarted)
            {
                m_DragStarted = true;
            }

            if ((direction & ResizerDirection.Right) != 0)
            {
                m_NewRect.width = Mathf.Min(m_MaxSize.x, Mathf.Max(m_MinSize.x, m_StartSize.x + mousePos.x - m_StartMouse.x));

                resizedTarget.style.width = m_NewRect.width;
            }
            else if ((direction & ResizerDirection.Left) != 0)
            {
                float delta = mousePos.x - m_StartMouse.x;

                if (m_StartSize.x - delta < m_MinSize.x)
                    delta = -m_MinSize.x + m_StartSize.x;
                else if (m_StartSize.x - delta > m_MaxSize.x)
                    delta = -m_MaxSize.x + m_StartSize.x;

                m_NewRect.x = delta + m_StartPosition.x;
                m_NewRect.width = -delta + m_StartSize.x;

                resizedTarget.style.left = m_NewRect.x;
                resizedTarget.style.width = m_NewRect.width;
            }

            if ((direction & ResizerDirection.Bottom) != 0)
            {
                m_NewRect.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));

                resizedTarget.style.height = m_NewRect.height;
            }
            else if ((direction & ResizerDirection.Top) != 0)
            {
                float delta = mousePos.y - m_StartMouse.y;

                if (m_StartSize.y - delta < m_MinSize.y)
                    delta = -m_MinSize.y + m_StartSize.y;
                else if (m_StartSize.y - delta > m_MaxSize.y)
                    delta = -m_MaxSize.y + m_StartSize.y;

                m_NewRect.y = delta + m_StartPosition.y;
                m_NewRect.height = -delta + m_StartSize.y;

                resizedTarget.style.top = m_NewRect.y;
                resizedTarget.style.height = m_NewRect.height;
            }

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!CanStopManipulation(e))
                return;

            if (m_Active)
            {
                VisualElement resizedTarget = resizedElement.parent;
                if (m_NewRect != new Rect(m_StartPosition, m_StartSize) &&
                    resizedTarget is GraphElement element &&
                    element.Model is IResizable resizable &&
                    element.Model.IsResizable())
                {
                    element.GraphView.Dispatch(new ChangeElementLayoutCommand(resizable, m_NewRect));
                }
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.ReleaseMouse();
                e.StopPropagation();

                m_Active = false;
            }
        }
    }
}
