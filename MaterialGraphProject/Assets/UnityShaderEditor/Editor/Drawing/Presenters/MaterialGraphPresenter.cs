using System;
using UnityEngine.MaterialGraph;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Graphing;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphObject : ScriptableObject
    {
        [SerializeField]
        public string serializedGraph;

        [SerializeField]
        public int version;
    }

    [Serializable]
    public class MaterialGraphPresenter : GraphViewPresenter
    {
        private GraphView m_GraphView;

        GraphTypeMapper typeMapper { get; set; }
        PreviewSystem m_PreviewSystem;

        public IGraph graph { get; private set; }

        [SerializeField]
        IMaterialGraphEditWindow m_Container;

        [SerializeField]
        MaterialGraphObject m_GraphObject;

        [SerializeField]
        int m_GraphVersion;

        protected MaterialGraphPresenter()
        {
#if WITH_PRESENTERS
            typeMapper = new GraphTypeMapper(typeof(MaterialNodePresenter));
            typeMapper[typeof(AbstractMaterialNode)] = typeof(MaterialNodePresenter);
            typeMapper[typeof(ColorNode)] = typeof(ColorNodePresenter);
            typeMapper[typeof(GradientNode)] = typeof(GradientNodePresenter);

            // typeMapper[typeof(ScatterNode)] = typeof(ScatterNodePresenter);
            //typeMapper[typeof(TextureNode)] = typeof(TextureNodePresenter);
            //typeMapper[typeof(SamplerAssetNode)] = typeof(SamplerAssetNodePresenter);
            //typeMapper[typeof(TextureSamplerNode)] = typeof(TextureSamplerNodePresenter);
//            typeMapper[typeof(Texture2DNode)] = typeof(TextureAssetNodePresenter);
 //           typeMapper[typeof(TextureLODNode)] = typeof(TextureLODNodePresenter);
            typeMapper[typeof(SamplerStateNode)] = typeof(SamplerStateNodePresenter);
   //         typeMapper[typeof(CubemapNode)] = typeof(CubeNodePresenter);
     //       typeMapper[typeof(ToggleNode)] = typeof(ToggleNodePresenter);
            typeMapper[typeof(UVNode)] = typeof(UVNodePresenter);
            typeMapper[typeof(Vector1Node)] = typeof(Vector1NodePresenter);
            typeMapper[typeof(Vector2Node)] = typeof(Vector2NodePresenter);
            typeMapper[typeof(Vector3Node)] = typeof(Vector3NodePresenter);
            typeMapper[typeof(Vector4Node)] = typeof(Vector4NodePresenter);
            typeMapper[typeof(PropertyNode)] = typeof(PropertyNodePresenter);

            /* typeMapper[typeof(ScaleOffsetNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
             typeMapper[typeof(RadialShearNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
             typeMapper[typeof(SphereWarpNode)] = typeof(AnyNodePresenter);          // anything derived from AnyNode should use the AnyNodePresenter
             typeMapper[typeof(SphericalIndentationNode)] = typeof(AnyNodePresenter);          // anything derived from AnyNode should use the AnyNodePresenter
             typeMapper[typeof(AACheckerboardNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter
             typeMapper[typeof(AACheckerboard3dNode)] = typeof(AnyNodePresenter);         // anything derived from AnyNode should use the AnyNodePresenter*/
            typeMapper[typeof(SubGraphNode)] = typeof(SubgraphNodePresenter);
            typeMapper[typeof(MasterRemapNode)] = typeof(MasterRemapNodePresenter);

            // typeMapper[typeof(MasterRemapInputNode)] = typeof(RemapInputNodePresenter);
            typeMapper[typeof(AbstractSubGraphIONode)] = typeof(SubgraphIONodePresenter);
//            typeMapper[typeof(AbstractSurfaceMasterNode)] = typeof(SurfaceMasterNodePresenter);
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
#else
            typeMapper = new GraphTypeMapper(typeof(MaterialNodeView));
            typeMapper[typeof(AbstractMaterialNode)] = typeof(MaterialNodeView);
            typeMapper[typeof(ColorNode)] = typeof(ColorNodeView);
            typeMapper[typeof(GradientNode)] = typeof(GradientNodeView);

            // typeMapper[typeof(ScatterNode)] = typeof(ScatterNodeView);
            //typeMapper[typeof(TextureNode)] = typeof(TextureNodeView);
            //typeMapper[typeof(SamplerAssetNode)] = typeof(SamplerAssetNodeView);
            //typeMapper[typeof(TextureSamplerNode)] = typeof(TextureSamplerNodeView);
            //            typeMapper[typeof(Texture2DNode)] = typeof(TextureAssetNodeView);
            //           typeMapper[typeof(TextureLODNode)] = typeof(TextureLODNodeView);
            typeMapper[typeof(SamplerStateNode)] = typeof(SamplerStateNodeView);
            //         typeMapper[typeof(CubemapNode)] = typeof(CubeNodeView);
            //       typeMapper[typeof(ToggleNode)] = typeof(ToggleNodeView);
            typeMapper[typeof(UVNode)] = typeof(UVNodeView);
            typeMapper[typeof(Vector1Node)] = typeof(Vector1NodeView);
            typeMapper[typeof(Vector2Node)] = typeof(Vector2NodeView);
            typeMapper[typeof(Vector3Node)] = typeof(Vector3NodeView);
            typeMapper[typeof(Vector4Node)] = typeof(Vector4NodeView);
            typeMapper[typeof(PropertyNode)] = typeof(PropertyNodeView);

            /* typeMapper[typeof(ScaleOffsetNode)] = typeof(AnyNodeView);         // anything derived from AnyNode should use the AnyNodeView
             typeMapper[typeof(RadialShearNode)] = typeof(AnyNodeView);         // anything derived from AnyNode should use the AnyNodeView
             typeMapper[typeof(SphereWarpNode)] = typeof(AnyNodeView);          // anything derived from AnyNode should use the AnyNodeView
             typeMapper[typeof(SphericalIndentationNode)] = typeof(AnyNodeView);          // anything derived from AnyNode should use the AnyNodeView
             typeMapper[typeof(AACheckerboardNode)] = typeof(AnyNodeView);         // anything derived from AnyNode should use the AnyNodeView
             typeMapper[typeof(AACheckerboard3dNode)] = typeof(AnyNodeView);         // anything derived from AnyNode should use the AnyNodeView*/
            typeMapper[typeof(SubGraphNode)] = typeof(SubgraphNodeView);
            typeMapper[typeof(MasterRemapNode)] = typeof(MasterRemapNodeView);

            // typeMapper[typeof(MasterRemapInputNode)] = typeof(RemapInputNodeView);
            typeMapper[typeof(AbstractSubGraphIONode)] = typeof(SubgraphIONodeView);
            //            typeMapper[typeof(AbstractSurfaceMasterNode)] = typeof(SurfaceMasterNodeView);
            typeMapper[typeof(LevelsNode)] = typeof(LevelsNodeView);
            typeMapper[typeof(ConstantsNode)] = typeof(ConstantsNodeView);

            //typeMapper[typeof(SwizzleNode)] = typeof(SwizzleNodeView);
            typeMapper[typeof(BlendModeNode)] = typeof(BlendModeNodeView);

            // typeMapper[typeof(AddManyNode)] = typeof(AddManyNodeView);
            typeMapper[typeof(IfNode)] = typeof(IfNodeView);

            //typeMapper[typeof(CustomCodeNode)] = typeof(CustomCodeView);
            typeMapper[typeof(Matrix2Node)] = typeof(Matrix2NodeView);
            typeMapper[typeof(Matrix3Node)] = typeof(Matrix3NodeView);
            typeMapper[typeof(Matrix4Node)] = typeof(Matrix4NodeView);
            typeMapper[typeof(MatrixCommonNode)] = typeof(MatrixCommonNodeView);
            typeMapper[typeof(TransformNode)] = typeof(TransformNodeView);

            //            typeMapper[typeof(ConvolutionFilterNode)] = typeof(ConvolutionFilterNodeView);
#endif
        }

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<NodeAnchorPresenter>();
            var startAnchorPresenter = startAnchor as GraphAnchorPresenter;
            if (startAnchorPresenter == null)
                return compatibleAnchors;
            var startSlot = startAnchorPresenter.slot as MaterialSlot;
            if (startSlot == null)
                return compatibleAnchors;

            var goingBackwards = startSlot.isOutputSlot;
            var startStage = startSlot.shaderStage;
            if (startStage == ShaderStage.Dynamic)
                startStage = NodeUtils.FindEffectiveShaderStage(startSlot.owner, startSlot.isOutputSlot);

            foreach (var candidateAnchorPresenter in allChildren.OfType<GraphAnchorPresenter>())
            {
                if (!candidateAnchorPresenter.IsConnectable())
                    continue;
                if (candidateAnchorPresenter.orientation != startAnchor.orientation)
                    continue;
                if (candidateAnchorPresenter.direction == startAnchor.direction)
                    continue;
                if (nodeAdapter.GetAdapter(candidateAnchorPresenter.source, startAnchor.source) == null)
                    continue;
                var candidateSlot = candidateAnchorPresenter.slot as MaterialSlot;
                if (candidateSlot == null)
                    continue;
                if (candidateSlot.owner == startSlot.owner)
                    continue;
                if (!startSlot.IsCompatibleWithInputSlotType(candidateSlot.valueType))
                    continue;

                if (startStage != ShaderStage.Dynamic)
                {
                    var candidateStage = candidateSlot.shaderStage;
                    if (candidateStage == ShaderStage.Dynamic)
                        candidateStage = NodeUtils.FindEffectiveShaderStage(candidateSlot.owner, !startSlot.isOutputSlot);
                    if (candidateStage != ShaderStage.Dynamic && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchorPresenter);
            }
            return compatibleAnchors;
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

                var theViews = m_GraphView.nodes.ToList().OfType<MaterialNodeView>();
                var viewsFound = theViews.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in viewsFound)
                    drawableNodeData.OnModified(scope);
            }

            // We might need to do something here
//            if (scope == ModificationScope.Topological)
        }

        public virtual void Initialize(GraphView graphView, IGraph graph, IMaterialGraphEditWindow container, PreviewSystem previewSystem)
        {
            m_GraphView = graphView;

            m_PreviewSystem = previewSystem;
            this.graph = graph;
            m_Container = container;

            if (graph == null)
                return;

            foreach (var node in graph.GetNodes<INode>())
                NodeAdded(new NodeAddedGraphChange(node));
            foreach (var edge in graph.edges)
                EdgeAdded(new EdgeAddedGraphChange(edge));

            this.graph.onChange += OnChange;

            m_GraphObject = CreateInstance<MaterialGraphObject>();
            Undo.undoRedoPerformed += UndoRedoPerformed;

            RecordState();
        }

        void UndoRedoPerformed()
        {
            if (m_GraphObject.version != m_GraphVersion)
            {
                var targetGraph = JsonUtility.FromJson(m_GraphObject.serializedGraph, graph.GetType()) as IGraph;
                graph.ReplaceWith(targetGraph);
                m_GraphVersion = m_GraphObject.version;
            }
        }

        void RecordState()
        {
            m_GraphObject.serializedGraph = JsonUtility.ToJson(graph, false);
            m_GraphObject.version++;
            m_GraphVersion = m_GraphObject.version;
        }

        void OnChange(GraphChange change)
        {
            change.Match(NodeAdded, NodeRemoved, EdgeAdded, EdgeRemoved);
        }

        void NodeAdded(NodeAddedGraphChange change)
        {
#if WITH_PRESENTER
            var nodePresenter = (MaterialNodePresenter)typeMapper.Create(change.node);
            change.node.onModified += OnNodeChanged;
            nodePresenter.Initialize(change.node, m_PreviewSystem);
            m_Elements.Add(nodePresenter);
#else
            var nodeView = (MaterialNodeView)typeMapper.Create(change.node);
            change.node.onModified += OnNodeChanged;
            nodeView.Initialize(change.node, m_PreviewSystem);
            m_GraphView.AddElement(nodeView);
#endif
        }

        void NodeRemoved(NodeRemovedGraphChange change)
        {
            change.node.onModified -= OnNodeChanged;

            var nodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(p => p.node.guid == change.node.guid);
            if (nodePresenter != null)
                m_Elements.Remove(nodePresenter);

            var nodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == change.node.guid);
            if (nodeView != null)
                m_GraphView.RemoveElement(nodeView);
        }

        void EdgeAdded(EdgeAddedGraphChange change)
        {
            var edge = change.edge;

            var sourceNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var sourceSlot = sourceNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);

            var targetNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var targetSlot = targetNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);

            var sourceNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == sourceNode);

            if (sourceNodePresenter != null)
            {
                var sourceAnchorPresenter = sourceNodePresenter.outputAnchors.OfType<GraphAnchorPresenter>().FirstOrDefault(x => x.slot.Equals(sourceSlot));

                var targetNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == targetNode);
                var targetAnchor = targetNodePresenter.inputAnchors.OfType<GraphAnchorPresenter>().FirstOrDefault(x => x.slot.Equals(targetSlot));

                var edgePresenter = CreateInstance<GraphEdgePresenter>();
                edgePresenter.Initialize(edge);
                edgePresenter.output = sourceAnchorPresenter;
                edgePresenter.output.Connect(edgePresenter);
                edgePresenter.input = targetAnchor;
                edgePresenter.input.Connect(edgePresenter);
                m_Elements.Add(edgePresenter);
            }
            else
            {
                var sourceNodeView = m_GraphView.nodes.ToList().OfType<MaterialNodeView>().FirstOrDefault(x => x.node == sourceNode);
                if (sourceNodeView == null)
                    return;

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

        void EdgeRemoved(EdgeRemovedGraphChange change)
        {
            var edgePresenter = m_Elements.OfType<GraphEdgePresenter>().FirstOrDefault(p => p.edge == change.edge);
            if (edgePresenter != null)
            {
                edgePresenter.output.Disconnect(edgePresenter);
                edgePresenter.input.Disconnect(edgePresenter);
                m_Elements.Remove(edgePresenter);
            }

            var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>().FirstOrDefault(p => p.userData is IEdge && (IEdge)p.userData == change.edge);
            if (edgeView != null)
            {
                edgeView.output.Disconnect(edgeView);
                edgeView.input.Disconnect(edgeView);
                m_GraphView.RemoveElement(edgeView);
            }
        }

        public void AddNode(INode node)
        {
            Undo.RecordObject(m_GraphObject, "Add " + node.name);
            graph.AddNode(node);
            RecordState();
        }

        public void RemoveElements(IEnumerable<MaterialNodePresenter> nodes, IEnumerable<GraphEdgePresenter> edges)
        {
            graph.RemoveElements(nodes.Select(x => x.node as INode), edges.Select(x => x.edge));
            graph.ValidateGraph();
        }

        public void RemoveElements(IEnumerable<MaterialNodeView> nodes, IEnumerable<Edge> edges)
        {
            graph.RemoveElements(nodes.Select(x => x.node as INode), edges.Select(x => x.userData as IEdge));  
            graph.ValidateGraph();
        }

        public void Connect(GraphAnchorPresenter left, GraphAnchorPresenter right)
        {
            if (left != null && right != null)
            {
                Undo.RecordObject(m_GraphObject, "Connect Edge");
                graph.Connect(left.slot.slotReference, right.slot.slotReference);
                RecordState();
            }
        }

        public void Connect(NodeAnchor left, NodeAnchor right)
        {
            if (left != null && right != null)
            {
                Undo.RecordObject(m_GraphObject, "Connect Edge");
                var leftSlot = left.userData as ISlot;
                var rightSlot = right.userData as ISlot;

                if (leftSlot == null || rightSlot == null)
                    return;

                graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
                RecordState();
            }
        }

        internal static CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElementPresenter> selection)
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

        void InsertCopyPasteGraph(CopyPasteGraph copyGraph)
        {
            if (copyGraph == null || graph == null)
                return;

            var addedNodes = new List<INode>();
            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in copyGraph.GetNodes<INode>())
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
                graph.AddNode(node);
                addedNodes.Add(node);
            }

            // only connect edges within pasted elements, discard
            // external edges.
            foreach (var edge in copyGraph.edges)
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
                    graph.Connect(outputSlotRef, inputSlotRef);
                }
            }

            graph.ValidateGraph();
            if (onSelectionChanged != null)
                onSelectionChanged(addedNodes);
        }

        public bool canCopy
        {
            get { return elements.Any(e => e.selected) || (m_GraphView != null && m_GraphView.selection.OfType<GraphElement>().Any(e => e.selected)); }
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
            Undo.RecordObject(m_GraphObject, "Cut");
            RemoveElements(elements.OfType<MaterialNodePresenter>().Where(e => e.selected), elements.OfType<GraphEdgePresenter>().Where(e => e.selected));
            RecordState();
        }

        public bool canPaste
        {
            get { return DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer) != null; }
        }

        public void Paste()
        {
            var pastedGraph = DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer);
            Undo.RecordObject(m_GraphObject, "Paste");
            InsertCopyPasteGraph(pastedGraph);
            RecordState();
        }

        public bool canDuplicate
        {
            get { return canCopy; }
        }

        public void Duplicate()
        {
            var graph = DeserializeCopyBuffer(JsonUtility.ToJson(CreateCopyPasteGraph(elements.Where(e => e.selected)), true));
            Undo.RecordObject(m_GraphObject, "Duplicate");
            InsertCopyPasteGraph(graph);
            RecordState();
        }

        public bool canDelete
        {
            get { return canCopy; }
        }

        public void Delete()
        {
            RecordState();
            Undo.RecordObject(m_GraphObject, "Delete");
            RemoveElements(elements.OfType<MaterialNodePresenter>().Where(e => e.selected), elements.OfType<GraphEdgePresenter>().Where(e => e.selected));
            RemoveElements(
                m_GraphView.selection.OfType<MaterialNodeView>().Where(e => e.selected && e.presenter == null),
                m_GraphView.selection.OfType<Edge>().Where(e => e.selected));
            RecordState();
        }

        public override void AddElement(EdgePresenter edge)
        {
            Connect(edge.output as GraphAnchorPresenter, edge.input as GraphAnchorPresenter);
        }

        public delegate void OnSelectionChanged(IEnumerable<INode> presenters);

        public OnSelectionChanged onSelectionChanged;

        public void UpdateSelection(IEnumerable<MaterialNodePresenter> presenters)
        {
            if (graph == null)
                return;
            if (onSelectionChanged != null)
                onSelectionChanged(presenters.Select(x => x.node as INode));
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
