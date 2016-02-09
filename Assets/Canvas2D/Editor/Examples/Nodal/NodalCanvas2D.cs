using UnityEngine;
using UnityEditor.Experimental;

#pragma warning disable 0414
#pragma warning disable 0219

namespace UnityEditor
{
    internal class NodalCanvas2D : EditorWindow
    {
        [MenuItem("Window/Canvas2D/Nodal UI")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(NodalCanvas2D));
        }

        private Canvas2D m_Canvas = null;
        private EditorWindow m_HostWindow = null;

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_Canvas = new Canvas2D(this, m_HostWindow, new NodalDataSource());

                // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
                m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
                m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

                // make the canvas zoomable
                m_Canvas.AddManipulator(new Zoomable());

                // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.All));
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.Selection));

                // The following manipulator show how to work with canvas2d overlay and background rendering
                m_Canvas.AddManipulator(new RectangleSelect());
                m_Canvas.AddManipulator(new ScreenSpaceGrid());
            }

            Rebuild();
        }

        private void Rebuild()
        {
            if (m_Canvas == null)
                return;

            m_Canvas.Clear();
            m_Canvas.ReloadData();
            m_Canvas.ZSort();
        }

        void OnGUI()
        {
            m_HostWindow = this;
            if (m_Canvas == null)
            {
                InitializeCanvas();
            }

            m_Canvas.OnGUI(this, new Rect(0, 0, position.width, position.height));
        }
    }
}
