using System;
using System.Linq;
using System.Reflection;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.RMGUI;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.Graphing.Drawing
{
    [StyleSheet("Assets/UnityShaderEditor/Editor/Styles/NodalView.uss")]
    //[StyleSheet("Assets/UnityShaderEditor/Editor/Styles/MatGraph.uss")]
    public class MaterialGraphView : GraphView
    {
        public MaterialGraphView()
        {
            // Shortcut handler to delete elements
            var dictionary = new Dictionary<Event, ShortcutDelegate>();
            dictionary[Event.KeyboardEvent("delete")] = DeleteSelection;
            contentViewContainer.AddManipulator(new ShortcutHandler(dictionary));

            AddManipulator(new ContentZoomer());
            AddManipulator(new ContentDragger());
            AddManipulator(new RectangleSelector());
            AddManipulator(new SelectionDragger());
            AddManipulator(new ClickSelector());
            AddManipulator(new ContextualMenu(DoContextMenu));
            AddDecorator(new GridBackground());

            dataMapper[typeof(MaterialNodeData)] = typeof(MaterialGraphNode);
            dataMapper[typeof(NodeAnchorData)] = typeof(NodeAnchor);
			dataMapper[typeof(EdgeData)] = typeof(RMGUI.GraphView.Edge);
        }

        private EventPropagation DeleteSelection()
        {
            var nodalViewData = dataSource as MaterialGraphDataSource;
            if (nodalViewData == null)
                return EventPropagation.Stop;

			nodalViewData.RemoveElements(
				selection.OfType<MaterialGraphNode>().Select(x => x.dataProvider as MaterialNodeData),
				selection.OfType<RMGUI.GraphView.Edge>().Select(x => x.dataProvider as MaterialEdgeData)
					);

			return EventPropagation.Stop;
        }

        public virtual bool CanAddToNodeMenu(Type type)
        {
            return true;
        }

        protected EventPropagation DoContextMenu(Event evt, Object customData)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0 && CanAddToNodeMenu(type))
                    {
                        gm.AddItem(new GUIContent(attrs[0].m_Title), false, AddNode, new AddNodeCreationObject(type, evt.mousePosition));
                    }
                }
            }

            //gm.AddSeparator("");
            // gm.AddItem(new GUIContent("Convert To/SubGraph"), true, ConvertSelectionToSubGraph);
            gm.ShowAsContext();
            return EventPropagation.Stop;
        }

        private class AddNodeCreationObject : object
        {
            public Vector2 m_Pos;
            public readonly Type m_Type;

            public AddNodeCreationObject(Type t, Vector2 p)
            {
                m_Type = t;
                m_Pos = p;
            }
        };

        private void AddNode(object obj)
        {
            var posObj = obj as AddNodeCreationObject;
            if (posObj == null)
                return;

            INode node;
            try
            {
                node = Activator.CreateInstance(posObj.m_Type) as INode;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("Could not construct instance of: {0} - {1}", posObj.m_Type, e);
                return;
            }

            if (node == null)
                return;
            var drawstate = node.drawState;
            drawstate.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, 0 , 0);
            node.drawState = drawstate;

            var graphDataSource = dataSource as MaterialGraphDataSource;
            graphDataSource.AddNode(node);
        }
    }
}
