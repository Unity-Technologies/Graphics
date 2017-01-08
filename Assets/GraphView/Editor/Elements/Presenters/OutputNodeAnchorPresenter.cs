using System;

namespace RMGUI.GraphView
{
	[Serializable]
	public class OutputNodeAnchorPresenter : NodeAnchorPresenter
	{
		public override Direction direction
		{
			get { return Direction.Output; }
		}
	}
}
