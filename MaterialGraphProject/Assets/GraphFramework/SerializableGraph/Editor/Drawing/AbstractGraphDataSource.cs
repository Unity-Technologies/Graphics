using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public abstract class AbstractGraphDataSource : ScriptableObject, IGraphElementDataSource
    {
        [SerializeField]
        private List<GraphElementData> m_Elements = new List<GraphElementData>();

        private readonly Dictionary<Type, Type> m_DataMapper = new Dictionary<Type, Type>();

        public IGraphAsset graphAsset { get; private set; }

        void OnNodeChanged(INode inNode)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theElements = m_Elements.OfType<NodeDrawData>().ToList();
                var found = theElements.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in found)
                    drawableNodeData.MarkDirtyHack();
            }
        }

        private void UpdateData()
        {
            var deletedElements = new List<GraphElementData>();

            // Find all nodes currently being drawn which are no longer in the graph (i.e. deleted)
            foreach (var nodeData in m_Elements.OfType<NodeDrawData>())
            {
                if (!graphAsset.graph.GetNodes<INode>().Contains(nodeData.node))
                {
                    // Mark node for deletion
                    deletedElements.Add(nodeData);
                }
            }

            // Find all edges currently being drawn which are no longer in the graph (i.e. deleted)
            foreach (var edgeData in m_Elements.OfType<EdgeDrawData>())
            {
                if (!graphAsset.graph.edges.Contains(edgeData.edge))
                {
                    // Mark edge for deletion
                    deletedElements.Add(edgeData);

                    // Make sure to disconnect the node, otherwise new connections won't be allowed for the used slots
                    edgeData.left.connected = false;
                    edgeData.right.connected = false;

                    var toNodeGuid = edgeData.edge.inputSlot.nodeGuid;
                    var toNode = m_Elements.OfType<NodeDrawData>().FirstOrDefault(nd => nd.node.guid == toNodeGuid);
                    if (toNode != null)
                    {
                        // Make the input node (i.e. right side of the connection) re-render
                        toNode.MarkDirtyHack();
                    }
                }
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

                var type = MapType(node.GetType());
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

                        var edgeData = ScriptableObject.CreateInstance<EdgeDrawData>();
                        edgeData.Initialize(edge);
                        edgeData.left = sourceAnchor;
                        edgeData.right = targetAnchor;
                        drawableEdges.Add(edgeData);
                    }
                }
            }

            // Add nodes marked for addition
            m_Elements.AddRange(addedNodes.OfType<GraphElementData>());

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

                    targetNode.MarkDirtyHack();

                    var edgeData = ScriptableObject.CreateInstance<EdgeDrawData>();
                    edgeData.Initialize(edge);
                    edgeData.left = sourceAnchor;
                    edgeData.right = targetAnchor;
                    drawableEdges.Add(edgeData);
                }
            }
            
            m_Elements.AddRange(drawableEdges.OfType<GraphElementData>());
        }

        private Type MapType(Type type)
        {
            Type found = null;
            while (type != null)
            {
                if (m_DataMapper.TryGetValue(type, out found))
                    break;
                type = type.BaseType;
            }
            return found ?? typeof(NodeDrawData);
        }

        protected abstract void AddTypeMappings();

        public void AddTypeMapping(Type node, Type drawData)
        {
            m_DataMapper[node] = drawData;
        }

        public virtual void Initialize(IGraphAsset graphAsset)
        {
            m_DataMapper.Clear();
            AddTypeMappings();

            this.graphAsset = graphAsset;

            if (graphAsset == null)
                return;

            UpdateData();
        }

        protected AbstractGraphDataSource()
        { }

        public void AddNode(INode node)
        {
            graphAsset.graph.AddNode(node);
            UpdateData();
        }

        public void RemoveElements(IEnumerable<NodeDrawData> nodes, IEnumerable<EdgeDrawData> edges)
        {
            graphAsset.graph.RemoveElements(nodes.Select(x => x.node), edges.Select(x => x.edge));
            graphAsset.graph.ValidateGraph();
            UpdateData();
        }

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Elements; }
        }

        public void AddElement(GraphElementData element)
        {
            var edge = element as EdgeData;
            if (edge.candidate == false)
            {
                var left = edge.left as AnchorDrawData;
                var right = edge.right as AnchorDrawData;
                if (left && right)
                    graphAsset.graph.Connect(left.slot.slotReference, right.slot.slotReference);
                UpdateData();
                return;
            }

            m_Elements.Add(element);
        }

        public void RemoveElement(GraphElementData element)
        {
            m_Elements.RemoveAll(x => x == element);
            UpdateData();
        }
    }
}
