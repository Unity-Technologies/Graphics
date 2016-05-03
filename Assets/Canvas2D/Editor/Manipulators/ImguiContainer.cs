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
            // This is needed for some reason, else control are not updated
            // TODO investigate!
            if (!target.selected)
                return false;

            if (evt.type == EventType.Repaint)
                return false;

            if (evt.type == EventType.MouseDown)
            {
                int a = 10;
            }
            EventType et = Event.current.type;

            CanvasElement topMostParent = target.FindTopMostParent();

            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;

            Rect screenRect = canvas.CanvasToScreen(topMostParent.canvasBoundingRect);
            Vector2 normalizedMouseOffset = (Event.current.mousePosition - screenRect.min);
            Vector2 screenWidth = (screenRect.max - screenRect.min);

            normalizedMouseOffset.x /= screenWidth.x;
            normalizedMouseOffset.y /= screenWidth.y;

            normalizedMouseOffset.x *= topMostParent.boundingRect.width;
            normalizedMouseOffset.y *= topMostParent.boundingRect.height;

            Vector2 currentMousePosition = Event.current.mousePosition;
            Event.current.mousePosition = normalizedMouseOffset;
            Event.current.type = et;

            topMostParent.Render(canvas.boundingRect, canvas);
            target.Invalidate();

            GUI.matrix = old;
            Event.current.mousePosition = currentMousePosition;
            return false;
        }
    }
}
