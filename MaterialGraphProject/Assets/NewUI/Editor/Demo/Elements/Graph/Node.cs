using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(NodeData))]
	class Node : SimpleElement
	{
		VisualContainer m_InputContainer;
		VisualContainer m_OutputContainer;

		public override void OnDataChanged()
		{
			base.OnDataChanged();

			m_OutputContainer.ClearChildren();
			m_InputContainer.ClearChildren();

			var nodeData = dataProvider as NodeData;

			if (nodeData != null)
			{
				foreach (var anchorData in nodeData.anchors)
				{
					m_InputContainer.AddChild(new NodeAnchor(anchorData));
				}
				m_OutputContainer.AddChild(new NodeAnchor(nodeData.outputAnchor));
			}
		}

		public Node()
		{
			m_InputContainer = new VisualContainer
			{
				name = "input", // for USS&Flexbox
				pickingMode = PickingMode.Ignore,
			};
			m_OutputContainer = new VisualContainer
			{
				name = "output", // for USS&Flexbox
				pickingMode = PickingMode.Ignore,
			};

			AddChild(m_InputContainer);
			AddChild(m_OutputContainer);
		}
	}
}
