using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class ImguiContainer : IManipulate
    {
        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.AllEvents += Render;
        }

        private bool Render(CanvasElement target, Event evt, Canvas2D canvas)
        {
            if (evt.type == EventType.Repaint)
                return false;

            EventType et = Event.current.type;

            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;

            Rect screenRect = canvas.CanvasToScreen(target.canvasBoundingRect);
            Vector2 normalizedMouseOffset = (Event.current.mousePosition - screenRect.min);
            Vector2 screenWidth = (screenRect.max - screenRect.min);

            normalizedMouseOffset.x /= screenWidth.x;
            normalizedMouseOffset.y /= screenWidth.y;

            normalizedMouseOffset.x *= target.boundingRect.width;
            normalizedMouseOffset.y *= target.boundingRect.height;

            Vector2 currentMousePosition = Event.current.mousePosition;
            Event.current.mousePosition = normalizedMouseOffset;
            Event.current.type = et;

            target.Render(canvas.boundingRect, canvas);
            target.Invalidate();

            GUI.matrix = old;
            return false;
        }
    }
}
