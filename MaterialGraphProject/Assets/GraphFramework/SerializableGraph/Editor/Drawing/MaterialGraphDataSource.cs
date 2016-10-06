using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
	[Serializable]
    class MaterialGraphDataSource : IDataSource
    {
        private List<GraphElementData> m_Elements = new List<GraphElementData>();

        public IGraphAsset graphAsset { get; private set; }

        public MaterialGraphDataSource(IGraphAsset graphAsset)
        {
            this.graphAsset = graphAsset;

            if (graphAsset == null)
                return;

			var drawableNodes = new List<MaterialNodeData>();
			foreach (var node in graphAsset.graph.GetNodes<INode>())
			{
				var nodeData = new MaterialNodeData(node);
				drawableNodes.Add(nodeData);
			}
			
			var drawableEdges = new List<EdgeData>();
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
						drawableEdges.Add(new EdgeData {Left = sourceAnchor, Right = targetAnchor});
					}
				}
			}

			m_Elements.AddRange(drawableNodes.OfType<GraphElementData>());
			m_Elements.AddRange(drawableEdges.OfType<GraphElementData>());
		}

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Elements; }
        }

		public void AddElement(GraphElementData element)
		{
			m_Elements.Add(element);
		}

		public void RemoveElement(GraphElementData element)
		{
			m_Elements.RemoveAll(x => x == element);
		}
	}
}
