using System;
using UnityEngine.MaterialGraph;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.MaterialGraph.Drawing
{
    [Serializable]
    public class MaterialGraphPresenter : GraphViewPresenter
    {
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

        private void UpdateData()
        {
            // Find all nodes currently being drawn which are no longer in the graph (i.e. deleted)
            var deletedElements = m_Elements
                .OfType<MaterialNodePresenter>()
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
                var toNode = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(nd => nd.node.guid == toNodeGuid);
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

            var addedNodes = new List<MaterialNodePresenter>();

            // Find all new nodes and mark for addition
            foreach (var node in graphAsset.graph.GetNodes<INode>())
            {
                // Check whether node already exists
                if (m_Elements.OfType<MaterialNodePresenter>().Any(e => e.node == node))
                    continue;

                var nodeData = (MaterialNodePresenter)typeMapper.Create(node);

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
                    var sourceAnchors = addedNode.outputAnchors.OfType<GraphAnchorPresenter>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == slot);

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = addedNodes.FirstOrDefault(x => x.node == toNode);
                        var targetAnchors = targetNode.inputAnchors.OfType<GraphAnchorPresenter>();
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
                    var sourceNode = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == fromNode);
                    var sourceAnchors = sourceNode.outputAnchors.OfType<GraphAnchorPresenter>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == fromSlot);

                    var toNode = graphAsset.graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                    var targetNode = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == toNode);
                    var targetAnchors = targetNode.inputAnchors.OfType<GraphAnchorPresenter>();
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

        private CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElementPresenter> selection)
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
