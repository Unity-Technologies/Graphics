using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;

namespace UnityEditor.MaterialGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public MaterialGraphView(EditorWindow editorWindow)
        {
            var shortcutHandler = new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
                {
                    {Event.KeyboardEvent("a"), FrameAll},
                    {Event.KeyboardEvent("f"), FrameSelection},
                    {Event.KeyboardEvent("o"), FrameOrigin},
                    {Event.KeyboardEvent("delete"), DeleteSelection},
                    {Event.KeyboardEvent("#tab"), FramePrev},
                    {Event.KeyboardEvent("tab"), FrameNext},
                    {Event.KeyboardEvent("#c"), CopySelection},
                    {Event.KeyboardEvent("#v"), Paste},
                    {Event.KeyboardEvent("#d"), DuplicateSelection}
                });

            onEnter += () => editorWindow.rootVisualContainer.parent.AddManipulator(shortcutHandler);
            onLeave += () => editorWindow.rootVisualContainer.parent.RemoveManipulator(shortcutHandler);

            this.AddManipulator(new Commandable
            {
                { "Duplicate", () => true, () => Debug.Log("Duplicate!") },
                { "Copy", () => true, () => Debug.Log("Copy!") }
            });

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new ClickSelector());

            Insert(0, new GridBackground());

            RegisterCallback<MouseUpEvent>(DoContextMenu);

            typeFactory[typeof(MaterialNodePresenter)] = typeof(MaterialNodeDrawer);
            typeFactory[typeof(GraphAnchorPresenter)] = typeof(NodeAnchor);
            typeFactory[typeof(EdgePresenter)] = typeof(Edge);

            AddStyleSheetPath("Styles/MaterialGraph");
        }

        public bool CanAddToNodeMenu(Type type)
        {
            return true;
        }

        void DoContextMenu(MouseUpEvent evt)
        {
            if (evt.button == (int)MouseButton.RightMouse)
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
            }
            evt.StopPropagation();
        }

        class AddNodeCreationObject
        {
            public Vector2 m_Pos;
            public readonly Type m_Type;

            public AddNodeCreationObject(Type t, Vector2 p)
            {
                m_Type = t;
                m_Pos = p;
            }
        };

        void AddNode(object obj)
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

            Vector3 localPos = contentViewContainer.transform.matrix.inverse.MultiplyPoint3x4(posObj.m_Pos);
            drawstate.position = new Rect(localPos.x, localPos.y, 0, 0);
            node.drawState = drawstate;

            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            graphDataSource.AddNode(node);
        }

        // TODO JOCE Remove the "new" here. Use the base class' impl
        new EventPropagation DeleteSelection()
        {
            var nodalViewData = GetPresenter<MaterialGraphPresenter>();
            if (nodalViewData == null)
                return EventPropagation.Stop;

            nodalViewData.RemoveElements(
                selection.OfType<MaterialNodeDrawer>().Select(x => x.GetPresenter<GraphNodePresenter>()),
                selection.OfType<Edge>().Select(x => x.GetPresenter<GraphEdgePresenter>())
                );

            return EventPropagation.Stop;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            if (graphDataSource == null)
                return;

            var graphAsset = graphDataSource.graphAsset;
            if (graphAsset == null || graphAsset.drawingData.selection.SequenceEqual(selection.OfType<MaterialNodeDrawer>().Select(d => ((GraphNodePresenter) d.presenter).node.guid))) return;

            var selectedDrawers = graphDataSource.graphAsset.drawingData.selection
                .Select(guid => contentViewContainer
                            .OfType<MaterialNodeDrawer>()
                            .FirstOrDefault(drawer => ((GraphNodePresenter) drawer.presenter).node.guid == guid))
                .ToList();

            ClearSelection();
            foreach (var drawer in selectedDrawers)
                AddToSelection(drawer);
        }

        void PropagateSelection()
        {
            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            if (graphDataSource == null)
                return;

            var selectedNodes = selection.OfType<MaterialNodeDrawer>().Select(x => (MaterialNodePresenter) x.presenter);
            graphDataSource.UpdateSelection(selectedNodes);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            PropagateSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            PropagateSelection();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            PropagateSelection();
        }

        public EventPropagation CopySelection()
        {
            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            if (selection.Any() && graphDataSource != null)
                graphDataSource.Copy(selection.OfType<GraphElement>().Select(ge => ge.presenter));
            return EventPropagation.Stop;
        }

        public EventPropagation DuplicateSelection()
        {
            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            if (selection.Any() && graphDataSource != null)
                graphDataSource.Duplicate(selection.OfType<GraphElement>().Select(ge => ge.presenter));
            return EventPropagation.Stop;
        }

        public EventPropagation Paste()
        {
            var graphDataSource = GetPresenter<MaterialGraphPresenter>();
            if (graphDataSource != null)
                graphDataSource.Paste();
            return EventPropagation.Stop;
        }
    }
}
