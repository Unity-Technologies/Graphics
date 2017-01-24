using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public interface IDroppable
	{
		bool IsDroppable();
	}

	public interface IDropTarget
	{
		bool CanAcceptDrop(List<ISelectable> selection);
		EventPropagation DragUpdated(Event evt, List<ISelectable> selection, IDropTarget dropTarget);
		EventPropagation DragPerform(Event evt, List<ISelectable> selection, IDropTarget dropTarget);
		EventPropagation DragExited();
	}
}
