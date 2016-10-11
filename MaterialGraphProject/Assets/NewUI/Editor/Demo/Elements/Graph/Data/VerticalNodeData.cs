using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	[CustomDataView(typeof(VerticalNode))]
	class VerticalNodeData : NodeData
	{
		// this class is useless, make a simple creation function
		protected new void OnEnable()
		{
			base.OnEnable();
			m_Anchors.Clear();

			var nodeAnchorData = CreateInstance<NodeAnchorData>();
			nodeAnchorData.orientation = Orientation.Vertical;
			nodeAnchorData.direction = Direction.Input;
			nodeAnchorData.type = typeof(float);
			m_Anchors.Add(nodeAnchorData);

			outputAnchor = CreateInstance<NodeAnchorData>();
			outputAnchor.type = typeof (float);
			outputAnchor.orientation = Orientation.Vertical;
			outputAnchor.direction = Direction.Output;
			outputAnchor.type = typeof(float);
		}
	}
}
