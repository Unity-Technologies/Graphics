#define DEBUG_MAT_GEN

using System;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private Vector2 m_ScrollPos;
        private bool m_NodeExpanded;

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
            if (Selection.activeObject == null || !EditorUtility.IsPersistent(Selection.activeObject))
                return;

            if (Selection.activeObject is MaterialGraph)
            {
                var selection = Selection.activeObject as MaterialGraph;
                if (selection != m_MaterialGraph)
                {
                    m_MaterialGraph = selection;
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
                m_Canvas.AddManipulator(new ContextualMenu(DoAddNodeMenu));
            }

            Rebuild();
        }

        private class AddNodeCreationObject : object
        {
            public Vector2 m_Pos;
            public readonly Type m_Type;

            public AddNodeCreationObject(Type t, Vector2 p) { m_Type = t; m_Pos = p; }
        };

        private void AddNode(object obj)
        {
            var posObj = obj as AddNodeCreationObject;
            if (posObj == null)
                return;

            var node = (BaseMaterialNode)CreateInstance(posObj.m_Type);
            node.OnCreate();
            node.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, node.position.width, node.position.height);
            m_MaterialGraph.currentGraph.AddNode(node);

            Rebuild();
            Repaint();
        }

        public virtual bool CanAddToNodeMenu(Type type) { return true; }
        protected bool DoAddNodeMenu(Event @event, Canvas2D parent, Object customData)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(BaseMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(BaseMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0 && CanAddToNodeMenu(type))
                    {
                        gm.AddItem(new GUIContent(attrs[0].m_Title), false, AddNode, new AddNodeCreationObject(type, parent.MouseToCanvas(@event.mousePosition)));
                    }
                }
            }
            gm.ShowAsContext();
            return true;
        }

        private void Rebuild()
        {
            if (m_Canvas == null || m_MaterialGraph == null)
                return;

            m_DataSource.graph = m_MaterialGraph;
            m_Canvas.ReloadData();
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
            m_Canvas.OnGUI(this, new Rect(0, 0, position.width - 250, position.height));
            RenderOptions(m_MaterialGraph);
        }

        public void RenderOptions(MaterialGraph graph)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, EditorStyles.textArea, GUILayout.Width(250), GUILayout.ExpandHeight(true));
            graph.materialOptions.DoGUI();
            EditorGUILayout.Separator();

            m_NodeExpanded = MaterialGraphStyles.Header("Selected", m_NodeExpanded);
            if (m_NodeExpanded)
                DrawableMaterialNode.OnGUI(m_Canvas.Selection);
      
            GUILayout.EndScrollView();
            if (GUILayout.Button("Export"))
                m_DataSource.Export(false);

            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        public static void DebugMaterialGraph(string s)
        {
#if DEBUG_MAT_GEN
            Debug.Log(s);
#endif
        }
    }
}
