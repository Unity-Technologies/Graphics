using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
using UnityEditor.MaterialGraph.Drawing.Inspector;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class GraphEditorView : VisualElement, IDisposable
    {
        AbstractMaterialGraph m_Graph;
        MaterialGraphView m_GraphView;
        GraphInspectorView m_GraphInspectorView;
        ToolbarView m_ToolbarView;
        ToolbarButtonView m_TimeButton;
        ToolbarButtonView m_CopyToClipboardButton;

        PreviewSystem m_PreviewSystem;

        public Action onUpdateAssetClick { get; set; }
        public Action onConvertToSubgraphClick { get; set; }
        public Action onShowInProjectClick { get; set; }

        public MaterialGraphView graphView
        {
            get { return m_GraphView; }
        }

        public PreviewRate previewRate
        {
            get { return previewSystem.previewRate; }
            set { previewSystem.previewRate = value; }
        }

        public PreviewSystem previewSystem
        {
            get { return m_PreviewSystem; }
            set { m_PreviewSystem = value; }
        }

        public GraphEditorView(AbstractMaterialGraph graph, Object asset)
        {
            m_Graph = graph;
            AddStyleSheetPath("Styles/MaterialGraph");

            previewSystem = new PreviewSystem(graph);

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
                        AbstractMaterialNode masterNode = graph.GetNodes<MasterNode>().First();
                        var textureInfo = new List<PropertyCollector.TextureInfo>();
                        PreviewMode previewMode;
                        string shader = graph.GetShader(masterNode, GenerationMode.ForReals, asset.name, out textureInfo, out previewMode);
                        GUIUtility.systemCopyBuffer = shader;
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
                content.Add(m_GraphView);

                m_GraphInspectorView = new GraphInspectorView(asset.name, previewSystem, graph) { name = "inspector" };
                m_GraphView.onSelectionChanged += m_GraphInspectorView.UpdateSelection;
                content.Add(m_GraphInspectorView);

                m_GraphView.graphViewChanged = GraphViewChanged;
            }

            foreach (var node in graph.GetNodes<INode>())
            {
                NodeAddedGraphChange change = new NodeAddedGraphChange(node);
                var nodeView = new MaterialNodeView(change.node as AbstractMaterialNode, m_PreviewSystem);
                nodeView.userData = change.node;
                change.node.onModified += OnNodeChanged;
                m_GraphView.AddElement(nodeView);
            }
            foreach (var edge in graph.edges)
            {
                var edge1 = new EdgeAddedGraphChange(edge).edge;

                var sourceNode = m_Graph.GetNodeFromGuid(edge1.outputSlot.nodeGuid);
                var sourceSlot = sourceNode.FindOutputSlot<ISlot>(edge1.outputSlot.slotId);

                var targetNode = m_Graph.GetNodeFromGuid(edge1.inputSlot.nodeGuid);
                var targetSlot = targetNode.FindInputSlot<ISlot>(edge1.inputSlot.slotId);

                var sourceNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == sourceNode);
                if (sourceNodeView != null)
                {
                    var sourceAnchor = sourceNodeView.outputContainer.Children().OfType<NodeAnchor>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(sourceSlot));

                    var targetNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == targetNode);
                    var targetAnchor = targetNodeView.inputContainer.Children().OfType<NodeAnchor>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(targetSlot));

                    var edgeView = new Edge();
                    edgeView.userData = edge1;
                    edgeView.output = sourceAnchor;
                    edgeView.output.Connect(edgeView);
                    edgeView.input = targetAnchor;
                    edgeView.input.Connect(edgeView);
                    m_GraphView.AddElement(edgeView);
                }
            }

            graph.onChange += OnGraphChange;

            Add(content);
        }

        GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                    var leftSlot = edge.output.userData as ISlot;
                    var rightSlot = edge.input.userData as ISlot;
                    if (leftSlot != null && rightSlot != null)
                        m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
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

        void OnGraphChange(GraphChange change)
        {
            var nodeAdded = change as NodeAddedGraphChange;
            if (nodeAdded != null)
            {
                var nodeView = new MaterialNodeView(nodeAdded.node as AbstractMaterialNode, m_PreviewSystem);
                nodeView.userData = nodeAdded.node;
                nodeAdded.node.onModified += OnNodeChanged;
                m_GraphView.AddElement(nodeView);
            }

            var nodeRemoved = change as NodeRemovedGraphChange;
            if (nodeRemoved != null)
            {
                nodeRemoved.node.onModified -= OnNodeChanged;

                var nodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == nodeRemoved.node.guid);
                if (nodeView != null)
                    m_GraphView.RemoveElement(nodeView);
            }

            var edgeAdded = change as EdgeAddedGraphChange;
            if (edgeAdded != null)
            {
                var edge = edgeAdded.edge;

                var sourceNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var sourceSlot = sourceNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);

                var targetNode = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                var targetSlot = targetNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);

                var sourceNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == sourceNode);
                if (sourceNodeView != null)
                {
                    var sourceAnchor = sourceNodeView.outputContainer.Children().OfType<NodeAnchor>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(sourceSlot));

                    var targetNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == targetNode);
                    var targetAnchor = targetNodeView.inputContainer.Children().OfType<NodeAnchor>().FirstOrDefault(x => x.userData is ISlot && (x.userData as ISlot).Equals(targetSlot));

                    var edgeView = new Edge();
                    edgeView.userData = edge;
                    edgeView.output = sourceAnchor;
                    edgeView.output.Connect(edgeView);
                    edgeView.input = targetAnchor;
                    edgeView.input.Connect(edgeView);
                    m_GraphView.AddElement(edgeView);
                }
            }

            var edgeRemoved = change as EdgeRemovedGraphChange;
            if (edgeRemoved != null)
            {
                var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>().FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, edgeRemoved.edge));
                if (edgeView != null)
                {
                    edgeView.output.Disconnect(edgeView);
                    edgeView.input.Disconnect(edgeView);
                    m_GraphView.RemoveElement(edgeView);
                }
            }
        }

        public void Dispose()
        {
            onUpdateAssetClick = null;
            onConvertToSubgraphClick = null;
            onShowInProjectClick = null;
            m_Graph.onChange -= OnGraphChange;
            if (m_GraphInspectorView != null) m_GraphInspectorView.Dispose();
            if (previewSystem != null)
            {
                previewSystem.Dispose();
                previewSystem = null;
            }
        }
    }
}
