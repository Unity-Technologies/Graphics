using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public abstract class AbstractGraphPresenter : GraphViewPresenter
    {
        protected TypeMapper dataMapper { get; set; }

        public IGraphAsset graphAsset { get; private set; }

        [SerializeField]
        private TitleBarPresenter m_TitleBar;

        [SerializeField]
        private EditorWindow m_Container;

        public TitleBarPresenter titleBar
        {
            get { return m_TitleBar; }
        }

        protected AbstractGraphPresenter()
        {
            dataMapper = new TypeMapper(typeof(GraphNodePresenter));
        }

        void OnNodeChanged(INode inNode, ModificationScope scope)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theElements = m_Elements.OfType<GraphNodePresenter>().ToList();
                var found = theElements.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in found)
                    drawableNodeData.OnModified(scope);
            }

            if (scope == ModificationScope.Topological)
                UpdateData();

            EditorUtility.SetDirty(graphAsset.GetScriptableObject());

            if (m_Container != null)
                m_Container.Repaint();
        }

        private void UpdateData()
        {
            // Find all nodes currently being drawn which are no longer in the graph (i.e. deleted)
            var deletedElements = m_Elements
                .OfType<GraphNodePresenter>()
                .Where(nd => !graphAsset.graph.GetNodes<INode>().Contains(nd.node))
                .OfType<GraphElementPresenter>()
                .ToList();

            var deletedEdges = m_Elements.OfType<GraphEdgePresenter>()
                .Where(ed => !graphAsset.graph.edges.Contains(ed.edge));

            // Find all edges currently being drawn which are no longer in the graph (i.e. deleted)
            foreach (var edgeData in deletedEdges)
            {
                // Make sure to disconnect the node, otherwise new connections won't be allowed for the used slots
                edgeData.output.Disconnect(edgeData);
                edgeData.input.Disconnect(edgeData);

                var toNodeGuid = edgeData.edge.inputSlot.nodeGuid;
                var toNode = m_Elements.OfType<GraphNodePresenter>().FirstOrDefault(nd => nd.node.guid == toNodeGuid);
                if (toNode != null)
                {
                    // Make the input node (i.e. right side of the connection) re-render
                    OnNodeChanged(toNode.node, ModificationScope.Graph);
                }

                deletedElements.Add(edgeData);
            }

            // Remove all nodes and edges marked for deletion
            foreach (var deletedElement in deletedElements)
            {
                m_Elements.Remove(deletedElement);
            }

            var addedNodes = new List<GraphNodePresenter>();

            // Find all new nodes and mark for addition
            foreach (var node in graphAsset.graph.GetNodes<INode>())
            {
                // Check whether node already exists
                if (m_Elements.OfType<GraphNodePresenter>().Any(e => e.node == node))
                    continue;

                var nodeData = (GraphNodePresenter)dataMapper.Create(node);

                node.onModified += OnNodeChanged;

                nodeData.Initialize(node);
                addedNodes.Add(nodeData);
            }

            // Create edge data for nodes marked for addition
            var drawableEdges = new List<GraphEdgePresenter>();
            foreach (var addedNode in addedNodes)
            {
                var baseNode = addedNode.node;
                foreach (var slot in baseNode.GetOutputSlots<ISlot>())
                {
                    var sourceAnchors = addedNode.elements.OfType<GraphAnchorPresenter>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == slot);

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = addedNodes.FirstOrDefault(x => x.node == toNode);
                        var targetAnchors = targetNode.elements.OfType<GraphAnchorPresenter>();
                        var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);

                        var edgeData = CreateInstance<GraphEdgePresenter>();
                        edgeData.Initialize(edge);
                        edgeData.output = sourceAnchor;
                        edgeData.output.Connect(edgeData);
                        edgeData.input = targetAnchor;
                        edgeData.input.Connect(edgeData);
                        drawableEdges.Add(edgeData);
                    }
                }
            }

            // Add nodes marked for addition
            m_Elements.AddRange(addedNodes.OfType<GraphElementPresenter>());

            // Find edges in the graph that are not being drawn and create edge data for them
            foreach (var edge in graphAsset.graph.edges)
            {
                if (!m_Elements.OfType<GraphEdgePresenter>().Any(ed => ed.edge == edge))
                {
                    var fromNode = graphAsset.graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    var fromSlot = fromNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);
                    var sourceNode = m_Elements.OfType<GraphNodePresenter>().FirstOrDefault(x => x.node == fromNode);
                    var sourceAnchors = sourceNode.elements.OfType<GraphAnchorPresenter>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == fromSlot);

                    var toNode = graphAsset.graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                    var targetNode = m_Elements.OfType<GraphNodePresenter>().FirstOrDefault(x => x.node == toNode);
                    var targetAnchors = targetNode.elements.OfType<GraphAnchorPresenter>();
                    var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);

                    OnNodeChanged(targetNode.node, ModificationScope.Graph);

                    var edgeData = CreateInstance<GraphEdgePresenter>();
                    edgeData.Initialize(edge);
                    edgeData.output = sourceAnchor;
                    edgeData.output.Connect(edgeData);
                    edgeData.input = targetAnchor;
                    edgeData.input.Connect(edgeData);
                    drawableEdges.Add(edgeData);
                }
            }

            m_Elements.AddRange(drawableEdges.OfType<GraphElementPresenter>());
        }

        public virtual void Initialize(IGraphAsset graphAsset, EditorWindow container)
        {
            this.graphAsset = graphAsset;
            m_Container = container;

            m_TitleBar = CreateInstance<TitleBarPresenter>();
            m_TitleBar.Initialize(graphAsset);

            if (graphAsset == null)
                return;

            UpdateData();
        }

        public void AddNode(INode node)
        {
            graphAsset.graph.AddNode(node);
            EditorUtility.SetDirty(graphAsset.GetScriptableObject());
            UpdateData();
        }

        public void RemoveElements(IEnumerable<GraphNodePresenter> nodes, IEnumerable<GraphEdgePresenter> edges)
        {
            graphAsset.graph.RemoveElements(nodes.Select(x => x.node), edges.Select(x => x.edge));
            graphAsset.graph.ValidateGraph();
            EditorUtility.SetDirty(graphAsset.GetScriptableObject());
            UpdateData();
        }

        public void Connect(GraphAnchorPresenter left, GraphAnchorPresenter right)
        {
            if (left != null && right != null)
            {
                graphAsset.graph.Connect(left.slot.slotReference, right.slot.slotReference);
                EditorUtility.SetDirty(graphAsset.GetScriptableObject());
                UpdateData();
            }
        }

        private CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElementPresenter> selection)
        {
            var graph = new CopyPasteGraph();
            foreach (var presenter in selection)
            {
                var nodePresenter = presenter as GraphNodePresenter;
                if (nodePresenter != null)
                {
                    graph.AddNode(nodePresenter.node);
                    foreach (var edge in NodeUtils.GetAllEdges(nodePresenter.node))
                        graph.AddEdge(edge);
                }

                var edgePresenter = presenter as GraphEdgePresenter;
                if (edgePresenter != null)
                    graph.AddEdge(edgePresenter.edge);
            }
            return graph;
        }

        private CopyPasteGraph DeserializeCopyBuffer(string copyBuffer)
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

        private void InsertCopyPasteGraph(CopyPasteGraph graph)
        {
            if (graph == null || graphAsset == null || graphAsset.graph == null)
                return;

            var addedNodes = new List<INode>();

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in graph.GetNodes<INode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;

                var drawState = node.drawState;
                var position = drawState.position;
                position.x += 30;
                position.y += 30;
                drawState.position = position;
                node.drawState = drawState;
                graphAsset.graph.AddNode(node);
                addedNodes.Add(node);
            }

            // only connect edges within pasted elements, discard
            // external edges.
            var addedEdges = new List<IEdge>();

            foreach (var edge in graph.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                if (nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid)
                    && nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid))
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    addedEdges.Add(graphAsset.graph.Connect(outputSlotRef, inputSlotRef));
                }
            }

            graphAsset.graph.ValidateGraph();
            UpdateData();

            graphAsset.drawingData.selection = addedNodes.Select(n => n.guid);
        }

        public void Copy(IEnumerable<GraphElementPresenter> selection)
        {
            var graph = CreateCopyPasteGraph(selection);
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(graph, true);
        }

        public void Duplicate(IEnumerable<GraphElementPresenter> selection)
        {
            var graph = DeserializeCopyBuffer(JsonUtility.ToJson(CreateCopyPasteGraph(selection), true));
            InsertCopyPasteGraph(graph);
        }

        public void Paste()
        {
            var pastedGraph = DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer);
            InsertCopyPasteGraph(pastedGraph);
        }

        public override void AddElement(EdgePresenter edge)
        {
            Connect(edge.output as GraphAnchorPresenter, edge.input as GraphAnchorPresenter);
        }

        public override void AddElement(GraphElementPresenter element)
        {
            throw new ArgumentException("Not supported on Serializable Graph, data comes from data store");
        }

        public override void RemoveElement(GraphElementPresenter element)
        {
            throw new ArgumentException("Not supported on Serializable Graph, data comes from data store");
        }
    }
}
