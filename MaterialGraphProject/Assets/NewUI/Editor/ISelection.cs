using System.Collections.Generic;

namespace RMGUI.GraphView
{
	public interface ISelection
	{
		List<ISelectable> selection { get; }

		void AddToSelection(ISelectable selectable);
		void RemoveFromSelection(ISelectable selectable);
		void ClearSelection();
	}
}
