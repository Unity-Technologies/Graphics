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
    public class MaterialGraphPresenter
    {
        private MaterialGraphView m_GraphView;

        PreviewSystem m_PreviewSystem;

        public IGraph graph { get; private set; }

        public MaterialGraphPresenter(MaterialGraphView graphView, IGraph graph, PreviewSystem previewSystem)
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

            this.graph.onChange += OnGraphChange;
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
                NodeAdded(nodeAdded);

            var nodeRemoved = change as NodeRemovedGraphChange;
            if (nodeRemoved != null)
                NodeRemoved(nodeRemoved);

            var edgeAdded = change as EdgeAddedGraphChange;
            if (edgeAdded != null)
                EdgeAdded(edgeAdded);

            var edgeRemoved = change as EdgeRemovedGraphChange;
            if (edgeRemoved != null)
                EdgeRemoved(edgeRemoved);
        }

        void NodeAdded(NodeAddedGraphChange change)
        {
            var nodeView = new MaterialNodeView(change.node as AbstractMaterialNode, m_PreviewSystem);
            nodeView.userData = change.node;
            change.node.onModified += OnNodeChanged;
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
            var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>().FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, change.edge));
            if (edgeView != null)
            {
                edgeView.output.Disconnect(edgeView);
                edgeView.input.Disconnect(edgeView);
                m_GraphView.RemoveElement(edgeView);
            }
        }
    }
}
