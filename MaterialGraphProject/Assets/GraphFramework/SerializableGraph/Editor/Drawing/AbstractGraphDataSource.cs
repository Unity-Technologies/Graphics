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
    public abstract class AbstractGraphDataSource : GraphViewPresenter
    {
        private readonly TypeMapper m_DataMapper = new TypeMapper(typeof(NodeDrawData));

        public IGraphAsset graphAsset { get; private set; }

        [SerializeField]
        private TitleBarDrawData m_TitleBar;

        public TitleBarDrawData titleBar
        {
            get { return m_TitleBar; }
        }

        void OnNodeChanged(INode inNode, ModificationScope scope)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theElements = m_Elements.OfType<NodeDrawData>().ToList();
                var found = theElements.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in found)
                    drawableNodeData.OnModified(scope);
            }

            if (scope == ModificationScope.Topological)
                UpdateData();

            EditorUtility.SetDirty(graphAsset.GetScriptableObject());
        }

        private void UpdateData()
        {
            // Find all nodes currently being drawn which are no longer in the graph (i.e. deleted)
            var deletedElements = m_Elements
                .OfType<NodeDrawData>()
                .Where(nd => !graphAsset.graph.GetNodes<INode>().Contains(nd.node))
                .OfType<GraphElementPresenter>()
                .ToList();

            var deletedEdges = m_Elements.OfType<EdgeDrawData>()
                .Where(ed => !graphAsset.graph.edges.Contains(ed.edge));

            // Find all edges currently being drawn which are no longer in the graph (i.e. deleted)
            foreach (var edgeData in deletedEdges)
            {
                // Make sure to disconnect the node, otherwise new connections won't be allowed for the used slots
                edgeData.output.Disconnect(edgeData);
                edgeData.input.Disconnect(edgeData);

                var toNodeGuid = edgeData.edge.inputSlot.nodeGuid;
                var toNode = m_Elements.OfType<NodeDrawData>().FirstOrDefault(nd => nd.node.guid == toNodeGuid);
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

            var addedNodes = new List<NodeDrawData>();

            // Find all new nodes and mark for addition
            foreach (var node in graphAsset.graph.GetNodes<INode>())
            {
                // Check whether node already exists
                if (m_Elements.OfType<NodeDrawData>().Any(e => e.node == node))
                    continue;

                var type = m_DataMapper.MapType(node.GetType());
                var nodeData = (NodeDrawData)CreateInstance(type);

                node.onModified += OnNodeChanged;

                nodeData.Initialize(node);
                addedNodes.Add(nodeData);
            }

            // Create edge data for nodes marked for addition
            var drawableEdges = new List<EdgeDrawData>();
            foreach (var addedNode in addedNodes)
            {
                var baseNode = addedNode.node;
                foreach (var slot in baseNode.GetOutputSlots<ISlot>())
                {
                    var sourceAnchors = addedNode.elements.OfType<AnchorDrawData>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == slot);

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = addedNodes.FirstOrDefault(x => x.node == toNode);
                        var targetAnchors = targetNode.elements.OfType<AnchorDrawData>();
                        var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);

                        var edgeData = CreateInstance<EdgeDrawData>();
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
                if (!m_Elements.OfType<EdgeDrawData>().Any(ed => ed.edge == edge))
                {
                    var fromNode = graphAsset.graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    var fromSlot = fromNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);
                    var sourceNode = m_Elements.OfType<NodeDrawData>().FirstOrDefault(x => x.node == fromNode);
                    var sourceAnchors = sourceNode.elements.OfType<AnchorDrawData>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == fromSlot);

                    var toNode = graphAsset.graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                    var targetNode = m_Elements.OfType<NodeDrawData>().FirstOrDefault(x => x.node == toNode);
                    var targetAnchors = targetNode.elements.OfType<AnchorDrawData>();
                    var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);

                    OnNodeChanged(targetNode.node, ModificationScope.Graph);

                    var edgeData = CreateInstance<EdgeDrawData>();
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

        protected abstract void AddTypeMappings(Action<Type, Type> map);

        public virtual void Initialize(IGraphAsset graphAsset)
        {
            m_DataMapper.Clear();
            AddTypeMappings(m_DataMapper.AddMapping);

            this.graphAsset = graphAsset;

            m_TitleBar = CreateInstance<TitleBarDrawData>();
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

        public void RemoveElements(IEnumerable<NodeDrawData> nodes, IEnumerable<EdgeDrawData> edges)
        {
            graphAsset.graph.RemoveElements(nodes.Select(x => x.node), edges.Select(x => x.edge));
            graphAsset.graph.ValidateGraph();
            EditorUtility.SetDirty(graphAsset.GetScriptableObject());
            UpdateData();
        }

        public void Connect(AnchorDrawData left, AnchorDrawData right)
        {
            if (left != null && right != null)
            {
                graphAsset.graph.Connect(left.slot.slotReference, right.slot.slotReference);
                EditorUtility.SetDirty(graphAsset.GetScriptableObject());
                UpdateData();
            }
        }

        public override void AddElement(EdgePresenter edge)
        {
            Connect(edge.output as AnchorDrawData, edge.input as AnchorDrawData);
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
