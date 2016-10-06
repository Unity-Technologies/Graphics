using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class MiniMapData : GraphElementData
	{
		protected new void OnEnable()
		{
			base.OnEnable();
			capabilities = Capabilities.Floating | Capabilities.Movable;
			maxWidth = 200;
			maxHeight = 200;
		}

		public float maxHeight;
		public float maxWidth;
	}
}
