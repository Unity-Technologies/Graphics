using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    [Serializable]
    public class MaterialGraphDataSource : ScriptableObject, IGraphElementDataSource
    {
        [SerializeField]
        private List<GraphElementData> m_Elements = new List<GraphElementData>();

        public IGraphAsset graphAsset { get; private set; }

        void OnNodeChanged(INode inNode)
        {
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theElements = m_Elements.OfType<MaterialNodeData>().ToList();
                var found = theElements.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in found)
                    drawableNodeData.MarkDirtyHack();
            }
        }

        private void UpdateData()
        {
            m_Elements.Clear();

            var drawableNodes = new List<MaterialNodeData>();
            foreach (var node in graphAsset.graph.GetNodes<INode>())
            {
                MaterialNodeData nodeData;

                if (node is ColorNode)
                    nodeData = ScriptableObject.CreateInstance<ColorNodeData>();
                else
                    nodeData = ScriptableObject.CreateInstance<MaterialNodeData>();

                node.onModified += OnNodeChanged;

                nodeData.Initialize(node);
                drawableNodes.Add(nodeData);
            }

            var drawableEdges = new List<MaterialEdgeData>();
            foreach (var addedNode in drawableNodes)
            {
                var baseNode = addedNode.node;
                foreach (var slot in baseNode.GetOutputSlots<ISlot>())
                {
                    var sourceAnchors = addedNode.elements.OfType<MaterialNodeAnchorData>();
                    var sourceAnchor = sourceAnchors.FirstOrDefault(x => x.slot == slot);

                    var edges = baseNode.owner.GetEdges(new SlotReference(baseNode.guid, slot.id));
                    foreach (var edge in edges)
                    {
                        var toNode = baseNode.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        var toSlot = toNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
                        var targetNode = drawableNodes.FirstOrDefault(x => x.node == toNode);

                        var targetAnchors = targetNode.elements.OfType<MaterialNodeAnchorData>();
                        var targetAnchor = targetAnchors.FirstOrDefault(x => x.slot == toSlot);
						var edgeData = ScriptableObject.CreateInstance<MaterialEdgeData>();
						edgeData.Initialize (edge);
                        edgeData.left = sourceAnchor;
                        edgeData.right = targetAnchor;
                        drawableEdges.Add(edgeData);
                    }
                }
            }

            m_Elements.AddRange(drawableNodes.OfType<GraphElementData>());
            m_Elements.AddRange(drawableEdges.OfType<GraphElementData>());
        }

        public MaterialGraphDataSource(IGraphAsset graphAsset)
        {
            this.graphAsset = graphAsset;

            if (graphAsset == null)
                return;

            UpdateData();
        }


        public void AddNode(INode node)
        {
            graphAsset.graph.AddNode(node);
            UpdateData();
        }

		public void RemoveElements(IEnumerable<MaterialNodeData> nodes, IEnumerable<MaterialEdgeData> edges) 
        {
			graphAsset.graph.RemoveElements(nodes.Select(x => x.node), edges.Select(x => x.edge));
			graphAsset.graph.ValidateGraph();
			UpdateData ();
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
				var left = edge.left as MaterialNodeAnchorData;
				var right = edge.right as MaterialNodeAnchorData;
				if (left && right)
					graphAsset.graph.Connect (left.slot.slotReference, right.slot.slotReference);
				UpdateData ();
				return;
			}
				
            m_Elements.Add(element);
        }

        public void RemoveElement(GraphElementData element)
        {
            m_Elements.RemoveAll(x => x == element);
        }
    }
}
