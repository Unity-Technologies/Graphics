using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	class VerticalNodePresenter : NodePresenter
	{
		public override Orientation orientation
		{
			get { return Orientation.Vertical; }
		}

		protected VerticalNodePresenter()
		{
		}
	}
}
