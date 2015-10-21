#define DEBUG_MAT_GEN

using UnityEditor.Experimental;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    class MaterialWindow : EditorWindow
    {
        [MenuItem("Window/Material")]
        public static void OpenMenu()
        {
            GetWindow<MaterialWindow>();
        }

        private MaterialGraph m_MaterialGraph;


        private Canvas2D m_Canvas = null;
        private EditorWindow m_HostWindow = null;
        private MaterialGraphDataSource m_DataSource;

        private bool shouldRepaint
        {
            get
            {
                return m_MaterialGraph != null && m_MaterialGraph.currentGraph != null && m_MaterialGraph.currentGraph.requiresRepaint;
            }
        }

        void Update()
        {
            if (shouldRepaint)
                Repaint();
        }

        void OnSelectionChange()
        {
            DebugMaterialGraph("Got OnSelection Change: " + Selection.activeObject);

            if (Selection.activeObject == null || !EditorUtility.IsPersistent(Selection.activeObject))
                return;

            if (Selection.activeObject is MaterialGraph)
            {
                var selection = Selection.activeObject as MaterialGraph;
                DebugMaterialGraph("Selection: " + selection);
                DebugMaterialGraph("Existing: " + m_MaterialGraph);
                if (selection != m_MaterialGraph)
                {
                    m_MaterialGraph = selection;
                    m_MaterialGraph.currentGraph.GeneratePreviewShaders();
                }
            }
            
            Rebuild();
            Repaint();
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = new MaterialGraphDataSource();
                m_Canvas = new Canvas2D(this, m_HostWindow, m_DataSource);

                // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
                m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
                m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

                // make the canvas zoomable
                m_Canvas.AddManipulator(new Zoomable());

                // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.eAll));
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.eSelection));

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

            m_DataSource.graph = m_MaterialGraph;
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

            if (m_MaterialGraph == null)
            {
                GUILayout.Label("No Graph selected");
                return;
            }

            //m_Canvas.dataSource = m_ActiveGraph;
            m_Canvas.OnGUI(this, new Rect(0, 0, position.width, position.height));
        }

        public static void DebugMaterialGraph(string s)
        {
#if DEBUG_MAT_GEN
            Debug.Log(s);
#endif
        }
    }
}
