using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	class VerticalNodeData : NodeData
	{
		// this class is useless, make a simple creation function
		protected new void OnEnable()
		{
			base.OnEnable();
			m_Anchors.Clear();

			var vna = CreateInstance<NodeAnchorData>();
			vna.orientation = Orientation.Vertical;
			vna.direction = Direction.Input;
			vna.type = typeof(float);
			m_Anchors.Add(vna);

			outputAnchor = CreateInstance<NodeAnchorData>();
			outputAnchor.type = typeof (float);
			outputAnchor.orientation = Orientation.Vertical;
			outputAnchor.direction = Direction.Output;
			outputAnchor.type = typeof(float);
		}
	}
}
