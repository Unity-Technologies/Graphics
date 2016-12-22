using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	class VerticalNodePresenter : NodePresenter
	{
		protected VerticalNodePresenter()
		{
			m_Orientation = Orientation.Vertical;
		}
	}
}
