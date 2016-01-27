using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class Zoomable : IManipulate
    {
        public enum ZoomType
        {
            AroundMouse = 0,
            LastClick = 1
        };

        public Zoomable()
        {
            m_Type = ZoomType.AroundMouse;
        }

        public Zoomable(ZoomType type)
        {
            m_Type = type;
        }

        private Vector2 m_ZoomLocation = Vector2.zero;
        private ZoomType m_Type;
        private float m_MinimumZoom = 0.08f;
        private float m_MaximumZoom = 1.0f;

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.ScrollWheel += OnZoom;
            element.KeyDown += OnKeyDown;

            if (m_Type == ZoomType.LastClick)
            {
                element.MouseDown += OnMouseDown;
            }
        }

        private bool OnMouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            m_ZoomLocation = e.mousePosition;
            m_ZoomLocation.x -= element.translation.x;
            m_ZoomLocation.y -= element.translation.y;
            return true;
        }

        private bool OnKeyDown(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (e.keyCode == KeyCode.R)
            {
                element.scale = Vector3.one;
                e.Use();
                return true;
            }
            return false;
        }

        private bool OnZoom(CanvasElement element, Event e, Canvas2D parent)
        {
            if (m_Type == ZoomType.AroundMouse)
            {
                m_ZoomLocation = e.mousePosition;
                m_ZoomLocation.x -= element.translation.x;
                m_ZoomLocation.y -= element.translation.y;
            }

            float delta = 0;
            delta += Event.current.delta.y;
            delta += Event.current.delta.x;
            delta = -delta;

            Vector3 currentScale = element.scale;
            Vector3 currentTranslation = element.translation;

            // Scale multiplier. Don't allow scale of zero or below!
            float scale = Mathf.Max(0.01F, 1 + delta * 0.01F);

            currentTranslation.x -= m_ZoomLocation.x * (scale - 1) * currentScale.x;
            currentScale.x *= scale;

            currentTranslation.y -= m_ZoomLocation.y * (scale - 1) * currentScale.y;
            currentScale.y *= scale;
            currentScale.z = 1.0f;

            bool outOfZoomBounds = false;
            if (((currentScale.x < m_MinimumZoom) || (currentScale.x > m_MaximumZoom)) ||
                ((currentScale.y < m_MinimumZoom) || (currentScale.y > m_MaximumZoom)))
            {
                outOfZoomBounds = true;
            }

            currentScale.x = Mathf.Clamp(currentScale.x, m_MinimumZoom, m_MaximumZoom);
            currentScale.y = Mathf.Clamp(currentScale.y, m_MinimumZoom, m_MaximumZoom);

            element.scale = currentScale;
            if (!outOfZoomBounds)
            {
                element.translation = currentTranslation;
            }

            e.Use();
            return true;
        }
    };
}
