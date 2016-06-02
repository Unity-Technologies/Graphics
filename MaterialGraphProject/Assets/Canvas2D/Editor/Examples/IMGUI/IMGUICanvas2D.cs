using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0414
#pragma warning disable 0219

namespace UnityEditor.Experimental.Graph.Examples
{
    internal class IMGUICanvas2D : EditorWindow
    {
        [MenuItem("Window/Canvas2D/IMGUI Coexistence Example")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IMGUICanvas2D));
        }

        private Canvas2D m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private List<CanvasElement> m_Data = new List<CanvasElement>();

        public void AddElement(CanvasElement e)
        {
            m_Data.Add(e);
            m_Canvas.ReloadData();

            var scaling = e.scale;
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_Canvas = new Canvas2D(this, m_HostWindow, new IMGUIDataSource(m_Data));

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

                AddElement(new IMGUIExampleWidget(new Vector2(0.0f, 250.0f), 300.0f));
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
