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
            element.AllEvents += (target, evt, canvas) =>
            {
                Vector2 canvasPos = canvas.MouseToCanvas(evt.mousePosition);
                Rect rect = canvas.CanvasToScreen(element.boundingRect);
                GUI.BeginGroup(rect);
                element.Render(canvas.boundingRect, canvas);
                GUI.EndGroup();

                canvas.Repaint();
                element.Invalidate();

                return false;
            };
        }
    }
}
