using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class GraphEditorView : VisualElement, IDisposable
    {
        MaterialGraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;

        AbstractMaterialGraph m_Graph;
        PreviewManager m_PreviewManager;
        SearchWindowProvider m_SearchWindowProvider;
        EdgeConnectorListener m_EdgeConnectorListener;

        public Action onUpdateAssetClick
        {
            get { return m_GraphInspectorView.onUpdateAssetClick; }
            set { m_GraphInspectorView.onUpdateAssetClick = value; }
        }

        public Action onConvertToSubgraphClick
        {
            get { return m_GraphView.onConvertToSubgraphClick; }
            set { m_GraphView.onConvertToSubgraphClick = value; }
        }

        public Action onShowInProjectClick
        {
            get { return m_GraphInspectorView.onShowInProjectClick; }
            set { m_GraphInspectorView.onShowInProjectClick = value; }
        }

        public MaterialGraphView graphView
        {
            get { return m_GraphView; }
        }

        PreviewManager previewManager
        {
            get { return m_PreviewManager; }
            set { m_PreviewManager = value; }
        }

        GraphInspectorView inspectorView
        {
            get { return m_GraphInspectorView; }
        }

        public GraphEditorView(EditorWindow editorWindow, AbstractMaterialGraph graph, string assetName)
        {
            m_Graph = graph;
            AddStyleSheetPath("Styles/MaterialGraph");

            previewManager = new PreviewManager(graph);

            var content = new VisualElement { name = "content" };
            {
                m_GraphView = new MaterialGraphView(graph) { name = "GraphView", persistenceKey = "MaterialGraphView" };
                m_GraphView.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                m_GraphView.AddManipulator(new ContentDragger());
                m_GraphView.AddManipulator(new SelectionDragger());
                m_GraphView.AddManipulator(new RectangleSelector());
                m_GraphView.AddManipulator(new ClickSelector());
                m_GraphView.AddManipulator(new GraphDropTarget(graph));
                content.Add(m_GraphView);

                m_GraphInspectorView = new GraphInspectorView(assetName, previewManager, graph) { name = "inspector" };
                m_GraphInspectorView.AddManipulator(new Draggable(OnMouseDrag, true));
                m_GraphView.RegisterCallback<PostLayoutEvent>(OnPostLayout);
                m_GraphInspectorView.RegisterCallback<PostLayoutEvent>(OnPostLayout);

                m_GraphView.Add(m_GraphInspectorView);

                m_GraphView.graphViewChanged = GraphViewChanged;
            }

            m_SearchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            m_SearchWindowProvider.Initialize(editorWindow, m_Graph, m_GraphView);
            m_GraphView.nodeCreationRequest = (c) =>
            {
                m_SearchWindowProvider.connectedPort = null;
                SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), m_SearchWindowProvider);
            };

            m_EdgeConnectorListener = new EdgeConnectorListener(m_Graph, m_SearchWindowProvider);

            foreach (var node in graph.GetNodes<INode>())
                AddNode(node);

            foreach (var edge in graph.edges)
                AddEdge(edge);

            Add(content);
        }

        void OnPostLayout(PostLayoutEvent evt)
        {
            const float minimumVisibility = 60f;

            Rect inspectorViewRect = m_GraphInspectorView.layout;

            float minimumXPosition = minimumVisibility - inspectorViewRect.width;
            float maximumXPosition = m_GraphView.layout.width - minimumVisibility;

            float minimumYPosition = minimumVisibility - inspectorViewRect.height;
            float maximumYPosition = m_GraphView.layout.height - minimumVisibility;

            inspectorViewRect.x = Mathf.Clamp(inspectorViewRect.x, minimumXPosition, maximumXPosition);
            inspectorViewRect.y = Mathf.Clamp(inspectorViewRect.y, minimumYPosition, maximumYPosition);

            inspectorViewRect.width = Mathf.Min(inspectorViewRect.width, layout.width);
            inspectorViewRect.height = Mathf.Min(inspectorViewRect.height, layout.height);

            m_GraphInspectorView.layout = inspectorViewRect;
        }

        void OnMouseDrag(Vector2 mouseDelta)
        {
            Vector2 normalizedDelta = mouseDelta / 2f;

            Rect inspectorWindowRect = m_GraphInspectorView.layout;

            inspectorWindowRect.x += normalizedDelta.x;
            inspectorWindowRect.y += normalizedDelta.y;

            m_GraphInspectorView.layout = inspectorWindowRect;
        }

        GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var leftSlot = edge.output.userData as ISlot;
                    var rightSlot = edge.input.userData as ISlot;
                    if (leftSlot != null && rightSlot != null)
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                        m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
                    }
                }
                graphViewChange.edgesToCreate.Clear();
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    var node = element.userData as INode;
                    if (node == null)
                        continue;

                    var drawState = node.drawState;
                    drawState.position = element.GetPosition();
                    node.drawState = drawState;
                }
            }

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            if (graphViewChange.elementsToRemove != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Remove Elements");
                m_Graph.RemoveElements(graphViewChange.elementsToRemove.OfType<MaterialNodeView>().Select(v => (INode) v.node),
                    graphViewChange.elementsToRemove.OfType<Edge>().Select(e => (IEdge) e.userData));
                foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    if (edge.input != null)
                    {
                        var materialNodeView = edge.input.node as MaterialNodeView;
                        if (materialNodeView != null)
                            nodesToUpdate.Add(materialNodeView);
                    }
                    if (edge.output != null)
                    {
                        var materialNodeView = edge.output.node as MaterialNodeView;
                        if (materialNodeView != null)
                            nodesToUpdate.Add(materialNodeView);
                    }
                }
            }

            foreach (var node in nodesToUpdate)
                node.UpdatePortInputVisibilities();

            UpdateEdgeColors(nodesToUpdate);

            return graphViewChange;
        }

        void OnNodeChanged(INode inNode, ModificationScope scope)
        {
            if (m_GraphView == null)
                return;

            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theViews = m_GraphView.nodes.ToList().OfType<MaterialNodeView>();
                var viewsFound = theViews.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in viewsFound)
                    drawableNodeData.OnModified(scope);
            }
        }

        HashSet<MaterialNodeView> m_NodeViewHashSet = new HashSet<MaterialNodeView>();

        public void HandleGraphChanges()
        {
            previewManager.HandleGraphChanges();
            previewManager.RenderPreviews();
            inspectorView.HandleGraphChanges();

            foreach (var node in m_Graph.removedNodes)
            {
                node.onModified -= OnNodeChanged;
                var nodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
                if (nodeView != null)
                {
                    nodeView.Dispose();
                    nodeView.userData = null;
                    m_GraphView.RemoveElement(nodeView);
                }
            }

            foreach (var node in m_Graph.addedNodes)
                AddNode(node);

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            foreach (var edge in m_Graph.removedEdges)
            {
                var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>().FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, edge));
                if (edgeView != null)
                {
                    var nodeView = edgeView.input.node as MaterialNodeView;
                    if (nodeView != null && nodeView.node != null)
                    {
                        nodesToUpdate.Add(nodeView);
                    }
                    edgeView.output.Disconnect(edgeView);
                    edgeView.input.Disconnect(edgeView);
                    m_GraphView.RemoveElement(edgeView);
                }
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var edgeView = AddEdge(edge);
                if (edgeView != null)
                    nodesToUpdate.Add((MaterialNodeView)edgeView.input.node);
            }

            foreach (var node in nodesToUpdate)
                node.UpdatePortInputVisibilities();

            UpdateEdgeColors(nodesToUpdate);
        }

        void AddNode(INode node)
        {
            var nodeView = new MaterialNodeView { userData = node };
            m_GraphView.AddElement(nodeView);
            nodeView.Initialize(node as AbstractMaterialNode, m_PreviewManager, m_EdgeConnectorListener);
            node.onModified += OnNodeChanged;
            nodeView.Dirty(ChangeType.Repaint);

            if (m_SearchWindowProvider.nodeNeedsRepositioning && m_SearchWindowProvider.targetSlotReference.nodeGuid.Equals(node.guid))
            {
                m_SearchWindowProvider.nodeNeedsRepositioning = false;
                foreach (var element in nodeView.inputContainer.Union(nodeView.outputContainer))
                {
                    var port = element as ShaderPort;
                    if (port == null)
                        continue;
                    var slot = (MaterialSlot)port.userData;
                    if (slot.slotReference.Equals(m_SearchWindowProvider.targetSlotReference))
                    {
                        port.RegisterCallback<PostLayoutEvent>(RepositionNode);
                        return;
                    }
                }
            }
        }

        static void RepositionNode(PostLayoutEvent evt)
        {
            var port = evt.target as ShaderPort;
            if (port == null)
                return;
            port.UnregisterCallback<PostLayoutEvent>(RepositionNode);
            var nodeView = port.node as MaterialNodeView;
            if (nodeView == null)
                return;
            var offset = nodeView.mainContainer.WorldToLocal(port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
            var position = nodeView.GetPosition();
            position.position -= offset;
            nodeView.SetPosition(position);
            var drawState = nodeView.node.drawState;
            drawState.position = position;
            nodeView.node.drawState = drawState;
            nodeView.Dirty(ChangeType.Repaint);
            port.Dirty(ChangeType.Repaint);
        }

        Edge AddEdge(IEdge edge)
        {
            var sourceNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            if (sourceNode == null)
            {
                Debug.LogWarning("Source node is null");
                return null;
            }
            var sourceSlot = sourceNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);

            var targetNode = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            if (targetNode == null)
            {
                Debug.LogWarning("Target node is null");
                return null;
            }
            var targetSlot = targetNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);

            var sourceNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == sourceNode);
            if (sourceNodeView != null)
            {
                var sourceAnchor = sourceNodeView.outputContainer.Children().OfType<Port>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(sourceSlot));

                var targetNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == targetNode);
                var targetAnchor = targetNodeView.inputContainer.Children().OfType<Port>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(targetSlot));

                var edgeView = new Edge
                {
                    userData = edge,
                    output = sourceAnchor,
                    input = targetAnchor
                };
                edgeView.output.Connect(edgeView);
                edgeView.input.Connect(edgeView);
                m_GraphView.AddElement(edgeView);
                sourceNodeView.RefreshPorts();
                targetNodeView.RefreshPorts();
                sourceNodeView.UpdatePortInputTypes();
                targetNodeView.UpdatePortInputTypes();

                return edgeView;
            }

            return null;
        }

        Stack<MaterialNodeView> m_NodeStack = new Stack<MaterialNodeView>();

        void UpdateEdgeColors(HashSet<MaterialNodeView> nodeViews)
        {
            var nodeStack = m_NodeStack;
            nodeStack.Clear();
            foreach (var nodeView in nodeViews)
                nodeStack.Push(nodeView);
            while (nodeStack.Any())
            {
                var nodeView = nodeStack.Pop();
                nodeView.UpdatePortInputTypes();
                foreach (var anchorView in nodeView.outputContainer.Children().OfType<Port>())
                {
                    foreach (var edgeView in anchorView.connections.OfType<Edge>())
                    {
                        var targetSlot = (MaterialSlot)edgeView.input.userData;
                        if (targetSlot.valueType == SlotValueType.Dynamic)
                        {
                            var connectedNodeView = edgeView.input.node as MaterialNodeView;
                            if (connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
                            {
                                nodeStack.Push(connectedNodeView);
                                nodeViews.Add(connectedNodeView);
                            }
                        }
                    }
                }
                foreach (var anchorView in nodeView.inputContainer.Children().OfType<Port>())
                {
                    var targetSlot = (MaterialSlot)anchorView.userData;
                    if (targetSlot.valueType != SlotValueType.Dynamic)
                        continue;
                    foreach (var edgeView in anchorView.connections.OfType<Edge>())
                    {
                        var connectedNodeView = edgeView.output.node as MaterialNodeView;
                        if (connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
                        {
                            nodeStack.Push(connectedNodeView);
                            nodeViews.Add(connectedNodeView);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            onUpdateAssetClick = null;
            onConvertToSubgraphClick = null;
            onShowInProjectClick = null;
            if (m_GraphView != null)
            {
                foreach (var node in m_GraphView.Children().OfType<MaterialNodeView>())
                    node.Dispose();
                m_GraphView = null;
            }
            if (m_GraphInspectorView != null) m_GraphInspectorView.Dispose();
            if (previewManager != null)
            {
                previewManager.Dispose();
                previewManager = null;
            }
            if (m_SearchWindowProvider != null)
            {
                Object.DestroyImmediate(m_SearchWindowProvider);
                m_SearchWindowProvider = null;
            }
        }
    }
}
