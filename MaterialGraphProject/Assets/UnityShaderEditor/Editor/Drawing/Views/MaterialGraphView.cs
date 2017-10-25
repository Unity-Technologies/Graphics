using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.Experimental.UIElements;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;

namespace UnityEditor.MaterialGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public MaterialGraphView()
        {
            RegisterCallback<MouseUpEvent>(DoContextMenu, Capture.Capture);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new ClickSelector());

            Insert(0, new GridBackground());

            AddStyleSheetPath("Styles/MaterialGraph");

            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
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

                gm.ShowAsContext();
            }
            evt.StopPropagation();
        }

        public override List<NodeAnchor> GetCompatibleAnchors(NodeAnchor startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<NodeAnchor>();
            var startSlot = startAnchor.userData as MaterialSlot;
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.shaderStage;
            if (startStage == ShaderStage.Dynamic)
                startStage = NodeUtils.FindEffectiveShaderStage(startSlot.owner, startSlot.isOutputSlot);

            foreach (var candidateAnchor in anchors.ToList())
            {
                if (!candidateAnchor.IsConnectable())
                    continue;
                if (candidateAnchor.orientation != startAnchor.orientation)
                    continue;
                if (candidateAnchor.direction == startAnchor.direction)
                    continue;
                if (nodeAdapter.GetAdapter(candidateAnchor.source, startAnchor.source) == null)
                    continue;
                var candidateSlot = candidateAnchor.userData as MaterialSlot;
                if (candidateSlot == null)
                    continue;
                if (candidateSlot.owner == startSlot.owner)
                    continue;
                if (!startSlot.IsCompatibleWithInputSlotType(candidateSlot.concreteValueType))
                    continue;

                if (startStage != ShaderStage.Dynamic)
                {
                    var candidateStage = candidateSlot.shaderStage;
                    if (candidateStage == ShaderStage.Dynamic)
                        candidateStage = NodeUtils.FindEffectiveShaderStage(candidateSlot.owner, !startSlot.isOutputSlot);
                    if (candidateStage != ShaderStage.Dynamic && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
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
                Debug.LogErrorFormat("Could not construct instance of: {0} - {1}", posObj.m_Type, e);
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

        void PropagateSelection()
        {
            var graphPresenter = GetPresenter<MaterialGraphPresenter>();
            if (graphPresenter == null)
                return;

            var selectedNodes = selection.OfType<MaterialNodeView>().Where(x => x.userData is INode);
            graphPresenter.UpdateSelection(selectedNodes);
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

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var graph = CreateCopyPasteGraph(elements);
            return JsonUtility.ToJson(graph, true);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            return DeserializeCopyBuffer(serializedData) != null;
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            var mgp = GetPresenter<MaterialGraphPresenter>();
            mgp.graph.owner.RegisterCompleteObjectUndo(operationName);
            var pastedGraph = DeserializeCopyBuffer(serializedData);
            mgp.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            var mgp = GetPresenter<MaterialGraphPresenter>();
            mgp.graph.owner.RegisterCompleteObjectUndo(operationName);
            mgp.RemoveElements(selection.OfType<MaterialNodeView>(), selection.OfType<Edge>());
        }

        internal static CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElement> selection)
        {
            var graph = new CopyPasteGraph();
            foreach (var element in selection)
            {
                var nodeView = element as MaterialNodeView;
                if (nodeView != null)
                {
                    graph.AddNode(nodeView.node);
                    foreach (var edge in NodeUtils.GetAllEdges(nodeView.userData as INode))
                        graph.AddEdge(edge);
                }

                var edgeView = element as Edge;
                if (edgeView != null)
                    graph.AddEdge(edgeView.userData as IEdge);
            }
            return graph;
        }

        internal static CopyPasteGraph DeserializeCopyBuffer(string copyBuffer)
        {
            try
            {
                return JsonUtility.FromJson<CopyPasteGraph>(copyBuffer);
            }
            catch
            {
                // ignored. just means copy buffer was not a graph :(
                return null;
            }
        }


    }
}
