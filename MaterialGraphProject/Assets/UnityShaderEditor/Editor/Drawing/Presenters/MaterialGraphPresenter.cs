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
    [Serializable]
    public class MaterialGraphPresenter : GraphViewPresenter
    {
        private GraphView m_GraphView;

        PreviewSystem m_PreviewSystem;

        public IGraph graph { get; private set; }

        protected MaterialGraphPresenter()
        {
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

            if (graph == null)
                return;

            foreach (var node in graph.GetNodes<INode>())
                NodeAdded(new NodeAddedGraphChange(node));
            foreach (var edge in graph.edges)
                EdgeAdded(new EdgeAddedGraphChange(edge));

            this.graph.onChange += OnChange;
        }

        void OnChange(GraphChange change)
        {
            change.Match(NodeAdded, NodeRemoved, EdgeAdded, EdgeRemoved);
        }

        void NodeAdded(NodeAddedGraphChange change)
        {
            var nodeView = new MaterialNodeView();
            change.node.onModified += OnNodeChanged;
            nodeView.Initialize(change.node, m_PreviewSystem);
            m_GraphView.AddElement(nodeView);
        }

        void NodeRemoved(NodeRemovedGraphChange change)
        {
            change.node.onModified -= OnNodeChanged;

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

        void EdgeRemoved(EdgeRemovedGraphChange change)
        {
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
            graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            graph.AddNode(node);
        }

        public void RemoveElements(IEnumerable<MaterialNodeView> nodes, IEnumerable<Edge> edges)
        {
            graph.RemoveElements(nodes.Select(x => x.node as INode), edges.Select(x => x.userData as IEdge));  
            graph.ValidateGraph();
        }

        public void Connect(NodeAnchor left, NodeAnchor right)
        {
            if (left != null && right != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                var leftSlot = left.userData as ISlot;
                var rightSlot = right.userData as ISlot;

                if (leftSlot == null || rightSlot == null)
                    return;

                graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
            }
        }

        internal static CopyPasteGraph CreateCopyPasteGraph(IEnumerable<GraphElement> selection)
        {
            var graph = new CopyPasteGraph();
            foreach (var element in selection)
            {
                var nodeView = element as MaterialNodeView;
                if (nodeView != null)
                {
                    graph.AddNode(nodeView.node);
                    foreach (var edge in NodeUtils.GetAllEdges(nodeView.userData as INode))
                        graph.AddEdge(edge);
                }

                var edgeView = element as Edge;
                if (edgeView != null)
                    graph.AddEdge(edgeView.userData as IEdge);
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
            var graph = CreateCopyPasteGraph(m_GraphView.selection.OfType<GraphElement>());
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(graph, true);
        }

        public bool canCut
        {
            get { return canCopy; }
        }

        public void Cut()
        {
            Copy();
            graph.owner.RegisterCompleteObjectUndo("Cut");
            RemoveElements(
                m_GraphView.selection.OfType<MaterialNodeView>(),
                m_GraphView.selection.OfType<Edge>());
        }

        public bool canPaste
        {
            get { return DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer) != null; }
        }

        public void Paste()
        {
            var pastedGraph = DeserializeCopyBuffer(EditorGUIUtility.systemCopyBuffer);
            graph.owner.RegisterCompleteObjectUndo("Paste");
            InsertCopyPasteGraph(pastedGraph);
        }

        public bool canDuplicate
        {
            get { return canCopy; }
        }

        public void Duplicate()
        {
            var deserializedGraph = DeserializeCopyBuffer(JsonUtility.ToJson(CreateCopyPasteGraph(m_GraphView.selection.OfType<GraphElement>()), true));
            graph.owner.RegisterCompleteObjectUndo("Duplicate");
            InsertCopyPasteGraph(deserializedGraph);
        }

        public bool canDelete
        {
            get { return canCopy; }
        }

        public void Delete()
        {
            graph.owner.RegisterCompleteObjectUndo("Delete");
            RemoveElements(
                m_GraphView.selection.OfType<MaterialNodeView>(),
                m_GraphView.selection.OfType<Edge>());
        }

        public delegate void OnSelectionChanged(IEnumerable<INode> nodes);

        public OnSelectionChanged onSelectionChanged;

        public void UpdateSelection(IEnumerable<MaterialNodeView> nodes)
        {
            if (graph == null)
                return;
            if (onSelectionChanged != null)
                onSelectionChanged(nodes.Select(x => x.userData as INode));
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
