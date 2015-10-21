using System;
using UnityEngine;
using System.Reflection;
using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    abstract class BaseMaterialGraphGUI : EditorWindow
    {
        internal const int kDefaultNodeWidth = 50;
        internal const int kDefaultNodeHeight = 80;

      /*  public override void OnGraphGUI()
        {
            m_Host.BeginWindows();

            // Process all nodes in the graph
            foreach (var n in graph.nodes)
            {
                var n2 = n;
                bool isSelected = selection.Contains(n2);
                n.position = GUILayout.Window(n.GetInstanceID(), n.position, delegate { NodeGUI(n2); }, n.title, Styles.GetNodeStyle(n.style, n.color, isSelected), GUILayout.Width(kDefaultNodeWidth), GUILayout.Height(kDefaultNodeHeight));
                if (n2 is BaseMaterialNode)
                    ((BaseMaterialNode)n2).isSelected = isSelected;
            }

            m_Host.EndWindows();

            edgeGUI.DoEdges();

            edgeGUI.DoDraggedEdge();

            DragSelection(new Rect(-5000, -5000, 10000, 10000));
            HandleMenuEvents();

            Event evt = Event.current;
            if (evt.type == EventType.MouseDown)
            {
                if (evt.button == 1)
                {
                    DoAddNodeMenu(Event.current.mousePosition);
                    evt.Use();
                }
            }
        }
        */
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
            node.Init();
            node.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, node.position.width, node.position.height);
           // graph.AddNode(node);
        }

        public virtual bool CanAddToNodeMenu(Type type) { return true; }

        protected void DoAddNodeMenu(Vector2 pos)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(BaseMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(BaseMaterialNode)) || type.IsSubclassOf(typeof(PropertyNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0 && CanAddToNodeMenu(type))
                    {
                        gm.AddItem(new GUIContent(attrs[0].m_Title), false, AddNode, new AddNodeCreationObject(type, pos));
                    }
                }
            }
            gm.ShowAsContext();
        }
    }
}
