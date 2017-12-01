using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class GraphEditorView : VisualElement, IDisposable
    {
        AbstractMaterialGraph m_Graph;
        MaterialGraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;
        ToolbarView m_ToolbarView;
        ToolbarButtonView m_TimeButton;
        ToolbarButtonView m_CopyToClipboardButton;

        PreviewManager m_PreviewManager;

        public Action onUpdateAssetClick { get; set; }
        public Action onConvertToSubgraphClick { get; set; }
        public Action onShowInProjectClick { get; set; }

        public MaterialGraphView graphView
        {
            get { return m_GraphView; }
        }

        public PreviewRate previewRate
        {
            get { return previewManager.previewRate; }
            set { previewManager.previewRate = value; }
        }

        public PreviewManager previewManager
        {
            get { return m_PreviewManager; }
            set { m_PreviewManager = value; }
        }

        public GraphInspectorView inspectorView
        {
            get { return m_GraphInspectorView; }
        }

        public GraphEditorView(AbstractMaterialGraph graph, string assetName)
        {
            m_Graph = graph;
            AddStyleSheetPath("Styles/MaterialGraph");

            previewManager = new PreviewManager(graph);

            m_ToolbarView = new ToolbarView { name = "TitleBar" };
            {
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var updateAssetButton = new ToolbarButtonView { text = "Update asset" };
                updateAssetButton.AddManipulator(new Clickable(() =>
                    {
                        if (onUpdateAssetClick != null) onUpdateAssetClick();
                    }));
                m_ToolbarView.Add(updateAssetButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var convertToSubgraphButton = new ToolbarButtonView { text = "Convert to subgraph" };
                convertToSubgraphButton.AddManipulator(new Clickable(() =>
                    {
                        if (onConvertToSubgraphClick != null) onConvertToSubgraphClick();
                    }));
                m_ToolbarView.Add(convertToSubgraphButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                var showInProjectButton = new ToolbarButtonView { text = "Show in project" };
                showInProjectButton.AddManipulator(new Clickable(() =>
                    {
                        if (onShowInProjectClick != null) onShowInProjectClick();
                    }));
                m_ToolbarView.Add(showInProjectButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
                m_ToolbarView.Add(new ToolbarSpaceView());
                m_ToolbarView.Add(new ToolbarSeparatorView());

                m_TimeButton = new ToolbarButtonView { text = "Preview rate: " + previewRate };
                m_TimeButton.AddManipulator(new Clickable(() =>
                    {
                        if (previewRate == PreviewRate.Full)
                            previewRate = PreviewRate.Throttled;
                        else if (previewRate == PreviewRate.Throttled)
                            previewRate = PreviewRate.Off;
                        else if (previewRate == PreviewRate.Off)
                            previewRate = PreviewRate.Full;
                        m_TimeButton.text = "Preview rate: " + previewRate;
                    }));
                m_ToolbarView.Add(m_TimeButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());

                m_CopyToClipboardButton = new ToolbarButtonView() { text = "Copy shader to clipboard" };
                m_CopyToClipboardButton.AddManipulator(new Clickable(() =>
                    {
                        AbstractMaterialNode copyFromNode = graph.GetNodes<MasterNode>().First();

                        if (graphView.selection.Count == 1)
                        {
                            MaterialNodeView selectedNodeView = graphView.selection[0] as MaterialNodeView;

                            if (selectedNodeView.node != null && selectedNodeView.node.hasPreview)
                            {
                                copyFromNode = selectedNodeView.node;
                            }
                        }

                        var textureInfo = new List<PropertyCollector.TextureInfo>();
                        PreviewMode previewMode;
                        FloatShaderProperty outputIdProperty;
                        if (copyFromNode is MasterNode)
                        {
                            var shader = ((MasterNode)copyFromNode).GetShader(GenerationMode.ForReals, copyFromNode.name, out textureInfo);
                            GUIUtility.systemCopyBuffer = shader;
                        }
                        else
                        {
                            string shader = graph.GetShader(copyFromNode, GenerationMode.ForReals, assetName, out textureInfo, out previewMode, out outputIdProperty);
                            GUIUtility.systemCopyBuffer = shader;
                        }
                    }
                        ));

                m_ToolbarView.Add(m_CopyToClipboardButton);

                m_ToolbarView.Add(new ToolbarSeparatorView());
            }
            Add(m_ToolbarView);

            var content = new VisualElement { name = "content" };
            {
                m_GraphView = new MaterialGraphView(graph) { name = "GraphView", persistenceKey = "MaterialGraphView" };
                m_GraphView.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                m_GraphView.AddManipulator(new ContentDragger());
                m_GraphView.AddManipulator(new RectangleSelector());
                m_GraphView.AddManipulator(new SelectionDragger());
                m_GraphView.AddManipulator(new ClickSelector());
                m_GraphView.AddManipulator(new NodeCreator(graph));
                m_GraphView.AddManipulator(new GraphDropTarget(graph));
                content.Add(m_GraphView);

                m_GraphInspectorView = new GraphInspectorView(assetName, previewManager, graph) { name = "inspector" };
                m_GraphView.onSelectionChanged += m_GraphInspectorView.UpdateSelection;
                content.Add(m_GraphInspectorView);

                m_GraphView.graphViewChanged = GraphViewChanged;
            }

            foreach (var node in graph.GetNodes<INode>())
                AddNode(node);

            foreach (var edge in graph.edges)
                AddEdge(edge);

            Add(content);
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
                    drawState.position = element.layout;
                    node.drawState = drawState;
                }
            }

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
            nodeView.Initialize(node as AbstractMaterialNode, m_PreviewManager);
            node.onModified += OnNodeChanged;
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
//                edgeView.UpdateClasses(sourceSlot.concreteValueType, targetSlot.concreteValueType);
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
                    var sourceSlot = (MaterialSlot)anchorView.userData;
                    foreach (var edgeView in anchorView.connections.OfType<Edge>())
                    {
                        var targetSlot = (MaterialSlot)edgeView.input.userData;
                        if (targetSlot.valueType == SlotValueType.Dynamic)
                        {
//                            edgeView.UpdateClasses(sourceSlot.concreteValueType, targetSlot.concreteValueType);
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
                        var sourceSlot = (MaterialSlot)edgeView.output.userData;
//                        edgeView.UpdateClasses(sourceSlot.concreteValueType, targetSlot.concreteValueType);
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
        }
    }
}
