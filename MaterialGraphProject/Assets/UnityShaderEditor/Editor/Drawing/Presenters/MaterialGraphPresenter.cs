using System;
using UnityEngine.MaterialGraph;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Graphing;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialGraphPresenter : GraphViewPresenter
    {
        HashSet<INode> m_TimeDependentNodes = new HashSet<INode>();

        protected GraphTypeMapper typeMapper { get; set; }

        public IGraphAsset graphAsset { get; private set; }

        [SerializeField]
        EditorWindow m_Container;

        [SerializeField]
        TitleBarPresenter m_TitleBar;

        public TitleBarPresenter titleBar
        {
            get { return m_TitleBar; }
        }

        public GraphInspectorPresenter graphInspectorPresenter
        {
            get { return m_GraphInspectorPresenter; }
            set { m_GraphInspectorPresenter = value; }
        }

        [SerializeField]
        GraphInspectorPresenter m_GraphInspectorPresenter;

        protected MaterialGraphPresenter()
        {
            typeMapper = new GraphTypeMapper(typeof(MaterialNodePresenter));
            typeMapper[typeof(AbstractMaterialNode)] = typeof(MaterialNodePresenter);
            typeMapper[typeof(ColorNode)] = typeof(ColorNodePresenter);
            typeMapper[typeof(GradientNode)] = typeof(GradientNodePresenter);
           // typeMapper[typeof(ScatterNode)] = typeof(ScatterNodePresenter);
            //typeMapper[typeof(TextureNode)] = typeof(TextureNodePresenter);
            //typeMapper[typeof(SamplerAssetNode)] = typeof(SamplerAssetNodePresenter);
            //typeMapper[typeof(TextureSamplerNode)] = typeof(TextureSamplerNodePresenter);
            typeMapper[typeof(Texture2DNode)] = typeof(TextureAssetNodePresenter);
            typeMapper[typeof(TextureLODNode)] = typeof(TextureLODNodePresenter);
            typeMapper[typeof(SamplerStateNode)] = typeof(SamplerStateNodePresenter);
            typeMapper[typeof(CubemapNode)] = typeof(CubeNodePresenter);
			typeMapper[typeof(ToggleNode)] = typeof(ToggleNodePresenter);
            typeMapper[typeof(UVNode)] = typeof(UVNodePresenter);
            typeMapper[typeof(Vector1Node)] = typeof(Vector1NodePresenter);
            typeMapper[typeof(Vector2Node)] = typeof(Vector2NodePresenter);
            typeMapper[typeof(Vector3Node)] = typeof(Vector3NodePresenter);
            typeMapper[typeof(Vector4Node)] = typeof(Vector4NodePresenter);
           /* typeMapper[typeof(ScaleOffsetNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(RadialShearNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(SphereWarpNode)] = typeof(AnyNodePresenter);          // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(SphericalIndentationNode)] = typeof(AnyNodePresenter);          // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(AACheckerboardNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
            typeMapper[typeof(AACheckerboard3dNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter*/
            typeMapper[typeof(SubGraphNode)] = typeof(SubgraphNodePresenter);
            typeMapper[typeof(RemapMasterNode)] = typeof(RemapMasterNodePresenter);
            typeMapper[typeof(MasterRemapInputNode)] = typeof(RemapInputNodePresenter);
            typeMapper[typeof(AbstractSubGraphIONode)] = typeof(SubgraphIONodePresenter);
            typeMapper[typeof(AbstractSurfaceMasterNode)] = typeof(SurfaceMasterNodePresenter);
            typeMapper[typeof(LevelsNode)] = typeof(LevelsNodePresenter);
            typeMapper[typeof(ConstantsNode)] = typeof(ConstantsNodePresenter);
            //typeMapper[typeof(SwizzleNode)] = typeof(SwizzleNodePresenter);
			typeMapper[typeof(BlendModeNode)] = typeof(BlendModeNodePresenter);
           // typeMapper[typeof(AddManyNode)] = typeof(AddManyNodePresenter);
            typeMapper[typeof(IfNode)] = typeof(IfNodePresenter);
            //typeMapper[typeof(CustomCodeNode)] = typeof(CustomCodePresenter);
            typeMapper[typeof(Matrix2Node)] = typeof(Matrix2NodePresenter);
            typeMapper[typeof(Matrix3Node)] = typeof(Matrix3NodePresenter);
            typeMapper[typeof(Matrix4Node)] = typeof(Matrix4NodePresenter);
            typeMapper[typeof(MatrixCommonNode)] = typeof(MatrixCommonNodePresenter);
			typeMapper[typeof(TransformNode)] = typeof(TransformNodePresenter);
//            typeMapper[typeof(ConvolutionFilterNode)] = typeof(ConvolutionFilterNodePresenter);
        }

		public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
			return allChildren.OfType<NodeAnchorPresenter>()
                              .Where(nap => nap.IsConnectable() &&
                                     nap.orientation == startAnchor.orientation &&
                                     nap.direction != startAnchor.direction &&
                                     nodeAdapter.GetAdapter(nap.source, startAnchor.source) != null &&
									(startAnchor is GraphAnchorPresenter && ((GraphAnchorPresenter)nap).slot is MaterialSlot &&
									((MaterialSlot)((GraphAnchorPresenter)startAnchor).slot).IsCompatibleWithInputSlotType(((MaterialSlot)((GraphAnchorPresenter)nap).slot).valueType)))
                              .ToList();
        }

        void OnNodeChanged(INode inNode, ModificationScope scope)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theElements = m_Elements.OfType<MaterialNodePresenter>().ToList();
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

        void UpdateData()
        {
            // Find all nodes currently being drawn which are no longer in the graph (i.e. deleted)
            var deletedElementPresenters = m_Elements
                .OfType<MaterialNodePresenter>()
                .Where(nd => !graphAsset.graph.GetNodes<INode>().Contains(nd.node))
                .OfType<GraphElementPresenter>()
                .ToList();

            var deletedEdgePresenters = m_Elements.OfType<GraphEdgePresenter>()
                .Where(ed => !graphAsset.graph.edges.Contains(ed.edge));

            // Find all edges currently being drawn which are no longer in the graph (i.e. deleted)
            foreach (var edgePresenter in deletedEdgePresenters)
            {
                // Make sure to disconnect the node, otherwise new connections won't be allowed for the used slots
                edgePresenter.output.Disconnect(edgePresenter);
                edgePresenter.input.Disconnect(edgePresenter);

                var fromNodeGuid = edgePresenter.edge.outputSlot.nodeGuid;
                var fromNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(nd => nd.node.guid == fromNodeGuid);

                var toNodeGuid = edgePresenter.edge.inputSlot.nodeGuid;
                var toNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(nd => nd.node.guid == toNodeGuid);

                if (toNodePresenter != null)
                    // Make the input node (i.e. right side of the connection) re-render
                    OnNodeChanged(toNodePresenter.node, ModificationScope.Graph);

                deletedElementPresenters.Add(edgePresenter);
            }

            // Remove all nodes and edges marked for deletion
            foreach (var elementPresenter in deletedElementPresenters)
            {
                m_Elements.Remove(elementPresenter);
            }

            var addedNodePresenters = new List<MaterialNodePresenter>();

            // Find all new nodes and mark for addition
            foreach (var node in graphAsset.graph.GetNodes<INode>())
            {
                // Check whether node already exists
                if (m_Elements.OfType<MaterialNodePresenter>().Any(e => e.node == node))
                    continue;

                var nodePresenter = (MaterialNodePresenter)typeMapper.Create(node);
                node.onModified += OnNodeChanged;
                nodePresenter.Initialize(node);
                addedNodePresenters.Add(nodePresenter);
            }

            // Create edge data for nodes marked for addition
            var edgePresenters = new List<GraphEdgePresenter>();
            foreach (var addedNodePresenter in addedNodePresenters)
            {
                var addedNode = addedNodePresenter.node;
                foreach (var slot in addedNode.GetOutputSlots<ISlot>())
                {
                    var sourceAnchors = addedNodePresenter.outputAnchors.OfType<GraphAnchorPresenter>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == slot);

                    var edges = addedNode.owner.GetEdges(new SlotReference(addedNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = addedNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = addedNodePresenters.FirstOrDefault(x => x.node == toNode);
                        var targetAnchors = targetNode.inputAnchors.OfType<GraphAnchorPresenter>();
                        var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);

                        var edgePresenter = CreateInstance<GraphEdgePresenter>();
                        edgePresenter.Initialize(edge);
                        edgePresenter.output = sourceAnchor;
                        edgePresenter.output.Connect(edgePresenter);
                        edgePresenter.input = targetAnchor;
                        edgePresenter.input.Connect(edgePresenter);
                        edgePresenters.Add(edgePresenter);
                    }
                }
            }

            // Add nodes marked for addition
            m_Elements.AddRange(addedNodePresenters.OfType<GraphElementPresenter>());

            // Find edges in the graph that are not being drawn and create edge data for them
            foreach (var edge in graphAsset.graph.edges)
            {
                if (m_Elements.OfType<GraphEdgePresenter>().Any(ed => ed.edge == edge))
                    continue;

                var sourceNode = graphAsset.graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var sourceSlot = sourceNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);
                var sourceNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == sourceNode);
                var sourceAnchorPresenters = sourceNodePresenter.outputAnchors.OfType<GraphAnchorPresenter>();
                var sourceAnchorPresenter = sourceAnchorPresenters.FirstOrDefault(x => x.slot == sourceSlot);

                var targetNode = graphAsset.graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                var targetSlot = targetNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                var targetNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == targetNode);
                var targetAnchors = targetNodePresenter.inputAnchors.OfType<GraphAnchorPresenter>();
                var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == targetSlot);

                OnNodeChanged(targetNodePresenter.node, ModificationScope.Graph);

                var edgePresenter = CreateInstance<GraphEdgePresenter>();
                edgePresenter.Initialize(edge);
                edgePresenter.output = sourceAnchorPresenter;
                edgePresenter.output.Connect(edgePresenter);
                edgePresenter.input = targetAnchor;
                edgePresenter.input.Connect(edgePresenter);
                edgePresenters.Add(edgePresenter);
            }

            m_Elements.AddRange(edgePresenters.OfType<GraphElementPresenter>());


            // Calculate which nodes require updates each frame (i.e. are time-dependent).

            // Let the node set contain all the nodes that are directly time-dependent.
            m_TimeDependentNodes.Clear();
            foreach (var node in graphAsset.graph.GetNodes<INode>().Where(x => x is IRequiresTime))
                m_TimeDependentNodes.Add(node);

            // The wavefront contains time-dependent nodes from which we wish to propagate time-dependency into the
            // nodes that it feeds into.
            var wavefront = new Stack<INode>(m_TimeDependentNodes);
            while (wavefront.Count > 0)
            {
                var node = wavefront.Pop();
                // Loop through all nodes that the node feeds into.
                foreach (var slot in node.GetOutputSlots<ISlot>())
                {
                    foreach (var edge in node.owner.GetEdges(slot.slotReference))
                    {
                        var inputNode = node.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        if (!m_TimeDependentNodes.Contains(inputNode))
                        {
                            // If the node is not in the set of time-dependent nodes, add it.
                            m_TimeDependentNodes.Add(inputNode);

                            // Also add it to the wavefront, such that we can process the nodes that it feeds into.
                            wavefront.Push(inputNode);
                        }
                    }
                }
            }

            // Update presenters `requiresTime` based on the hash set values.
            foreach (var nodePresenter in m_Elements.OfType<MaterialNodePresenter>())
                nodePresenter.requiresTime = m_TimeDependentNodes.Contains(nodePresenter.node);
        }

        public virtual void Initialize(IGraphAsset graphAsset, MaterialGraphEditWindow container)
        {
            this.graphAsset = graphAsset;
            m_Container = container;

            m_TitleBar = CreateInstance<TitleBarPresenter>();
			m_TitleBar.Initialize(container);

            m_GraphInspectorPresenter = CreateInstance<GraphInspectorPresenter>();
            m_GraphInspectorPresenter.Initialize();

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

        public void RemoveElements(IEnumerable<MaterialNodePresenter> nodes, IEnumerable<GraphEdgePresenter> edges)
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

        CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElementPresenter> selection)
        {
            var graph = new CopyPasteGraph();
            foreach (var presenter in selection)
            {
                var nodePresenter = presenter as MaterialNodePresenter;
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

        CopyPasteGraph DeserializeCopyBuffer(string copyBuffer)
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

        void InsertCopyPasteGraph(CopyPasteGraph graph)
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

        public bool canCopy
        {
            get { return elements.Any(e => e.selected); }
        }

        public void Copy()
        {
            var graph = CreateCopyPasteGraph(elements.Where(e => e.selected));
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(graph, true);
        }

        public bool canCut
        {
            get { return canCopy; }
        }

        public void Cut()
        {
            Copy();
            RemoveElements(elements.OfType<MaterialNodePresenter>().Where(e => e.selected), elements.OfType<GraphEdgePresenter>().Where(e => e.selected));
        }

        public bool canPaste
        {
            get { return DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer) != null; }
        }

        public void Paste()
        {
            var pastedGraph = DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer);
            InsertCopyPasteGraph(pastedGraph);
        }

        public bool canDuplicate
        {
            get { return canCopy; }
        }

        public void Duplicate()
        {
            var graph = DeserializeCopyBuffer(JsonUtility.ToJson(CreateCopyPasteGraph(elements.Where(e => e.selected)), true));
            InsertCopyPasteGraph(graph);
        }

        public bool canDelete
        {
            get { return canCopy; }
        }

        public void Delete()
        {
            RemoveElements(elements.OfType<MaterialNodePresenter>().Where(e => e.selected), elements.OfType<GraphEdgePresenter>().Where(e => e.selected));
        }

        public override void AddElement(EdgePresenter edge)
        {
            Connect(edge.output as GraphAnchorPresenter, edge.input as GraphAnchorPresenter);
        }

        public void UpdateSelection(IEnumerable<MaterialNodePresenter> presenters)
        {
            if (graphAsset == null)
                return;
            graphAsset.drawingData.selection = presenters.Select(x => x.node.guid);
            m_GraphInspectorPresenter.UpdateSelection(presenters.Select(x => x.node));
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
