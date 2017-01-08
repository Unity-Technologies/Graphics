using System;

namespace RMGUI.GraphView
{
	[Serializable]
	public class InputNodeAnchorPresenter : NodeAnchorPresenter
	{
		public override Direction direction
		{
			get { return Direction.Input; }
		}
	}
}
