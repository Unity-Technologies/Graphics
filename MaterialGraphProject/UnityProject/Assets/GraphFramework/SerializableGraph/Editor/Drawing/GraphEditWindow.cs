using System;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.Graphing.Drawing
{
    class GraphEditWindow : EditorWindow
    {
        [MenuItem("Window/Graph Editor")]
        public static void OpenMenu()
        {
            GetWindow<GraphEditWindow>();
        }

        [SerializeField]
        private SerializableGraphAsset m_LastSelection;
        
        [NonSerialized]
        private Canvas2D m_Canvas;
        [NonSerialized]
        private EditorWindow m_HostWindow;
        [NonSerialized]
        private GraphDataSource m_DataSource;
        
        private bool shouldRepaint
        {
            get
            {
                return m_LastSelection != null && m_LastSelection.graph != null;
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
            
            if (Selection.activeObject is SerializableGraphAsset)
            {
                var selection = (SerializableGraphAsset) Selection.activeObject;
                if (selection != m_LastSelection)
                {
                    m_LastSelection = selection;
                    Rebuild();
                    Repaint();
                }
            }
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = new GraphDataSource();
                m_Canvas = new Canvas2D(this, m_HostWindow, m_DataSource);

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
                m_Canvas.AddManipulator(new ContextualMenu(DoAddNodeMenu));
                
                m_Canvas.AddManipulator(new DeleteSelected(m_DataSource.DeleteElements, m_Canvas));
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

            INode node;
            try
            {
                node = Activator.CreateInstance(posObj.m_Type, m_LastSelection.graph) as INode;
            }
            catch
            {
                Debug.LogWarningFormat("Could not construct instance of: {0}", posObj.m_Type);
                return;
            }

            if (node == null)
                return;
            var drawstate = node.drawState;
            drawstate.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, drawstate.position.width, drawstate.position.height);
            node.drawState = drawstate;
            m_LastSelection.graph.AddNode(node);
            EditorUtility.SetDirty(m_LastSelection);
            Rebuild();
            Repaint();
        }

        public virtual bool CanAddToNodeMenu(Type type) { return true; }
        protected bool DoAddNodeMenu(Event @event, Canvas2D parent, Object customData)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
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
            if (m_Canvas == null || m_LastSelection == null || m_LastSelection.graph == null)
                return;

            m_DataSource.graph = m_LastSelection.graph;
            m_Canvas.ReloadData();
        }

        void OnGUI()
        {
            m_HostWindow = this;
            if (m_Canvas == null)
            {
                InitializeCanvas();
            }

            if (m_LastSelection == null ||  m_LastSelection.graph == null)
            {
                GUILayout.Label("No Graph selected");
                return;
            }
            
            m_Canvas.OnGUI(this, new Rect(0, 0, position.width - 250, position.height));
        }

        /*public void RenderOptions(MaterialGraph graph)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, EditorStyles.textArea, GUILayout.Width(250), GUILayout.ExpandHeight(true));
            graph.materialOptions.DoGUI();
            EditorGUILayout.Separator();

            m_NodeExpanded = MaterialGraphStyles.Header("Selected", m_NodeExpanded);
            if (m_NodeExpanded)
                DrawableMaterialNode.OnGUI(m_Canvas.selection);
      
            GUILayout.EndScrollView();
            if (GUILayout.Button("Export"))
                m_DataSource.Export(false);

            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }*/
    }
}
