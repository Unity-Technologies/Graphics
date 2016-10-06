using System;
using System.Collections.Generic;
using System.Linq;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
	[Serializable]
	public class MaterialNodeAnchorData : NodeAnchorData
	{
		public ISlot slot { get; private set; }

		public MaterialNodeAnchorData(ISlot slot)
		{
			this.slot = slot;
			name = slot.displayName;
			type = typeof(Vector4);
			direction = slot.isInputSlot ?  Direction.Input : Direction.Output;
		}
	}

	[Serializable]
	public class MaterialNodeData : GraphElementData
	{
        public INode node { get; private set; }

        protected List<NodeAnchorData> m_Anchors = new List<NodeAnchorData>();

        public IEnumerable<GraphElementData> elements
        {
            get { return m_Anchors.OfType<GraphElementData>(); }
        }

        public MaterialNodeData(INode inNode)
        {
            node = inNode;

            if (node == null)
                return;

	        name = inNode.name;

            foreach (var input in node.GetSlots<ISlot>())
            {
                m_Anchors.Add(new MaterialNodeAnchorData(input));
            }
			
            position = new Rect(node.drawState.position.x, node.drawState.position.y, 100, 200);
            capabilities |= Capabilities.Movable;
        }

    }
}
