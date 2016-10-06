using System.Collections.Generic;

namespace RMGUI.GraphView
{
	public interface ISelection
	{
		List<ISelectable> selection { get; }

		void AddToSelection(ISelectable e);
		void RemoveFromSelection(ISelectable e);
		void ClearSelection();
	}
}
