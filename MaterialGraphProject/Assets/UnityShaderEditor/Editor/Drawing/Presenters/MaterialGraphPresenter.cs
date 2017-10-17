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
            typeMapper[typeof(SubGraphOutputNode)] = typeof(SubgraphIONodePresenter);
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
            }

            // We might need to do something here
//            if (scope == ModificationScope.Topological)
        }

        public virtual void Initialize(IGraph graph, IMaterialGraphEditWindow container, PreviewSystem previewSystem)
        {
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
            var nodePresenter = (MaterialNodePresenter)typeMapper.Create(change.node);
            change.node.onModified += OnNodeChanged;
            nodePresenter.Initialize(change.node, m_PreviewSystem);
            m_Elements.Add(nodePresenter);
        }

        void NodeRemoved(NodeRemovedGraphChange change)
        {
            change.node.onModified -= OnNodeChanged;
            var nodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(p => p.node.guid == change.node.guid);
            if (nodePresenter != null)
                m_Elements.Remove(nodePresenter);
        }

        void EdgeAdded(EdgeAddedGraphChange change)
        {
            var edge = change.edge;

            var sourceNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var sourceSlot = sourceNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);
            var sourceNodePresenter = m_Elements.OfType<MaterialNodePresenter>().FirstOrDefault(x => x.node == sourceNode);
            var sourceAnchorPresenter = sourceNodePresenter.outputAnchors.OfType<GraphAnchorPresenter>().FirstOrDefault(x => x.slot.Equals(sourceSlot));

            var targetNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var targetSlot = targetNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
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

        void EdgeRemoved(EdgeRemovedGraphChange change)
        {
            var edgePresenter = m_Elements.OfType<GraphEdgePresenter>().FirstOrDefault(p => p.edge == change.edge);
            if (edgePresenter != null)
            {
                edgePresenter.output.Disconnect(edgePresenter);
                edgePresenter.input.Disconnect(edgePresenter);
                m_Elements.Remove(edgePresenter);
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

        public void Connect(GraphAnchorPresenter left, GraphAnchorPresenter right)
        {
            if (left != null && right != null)
            {
                Undo.RecordObject(m_GraphObject, "Connect Edge");
                graph.Connect(left.slot.slotReference, right.slot.slotReference);
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
