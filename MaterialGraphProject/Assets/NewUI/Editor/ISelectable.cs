using UnityEngine;

namespace RMGUI.GraphView
{
	public interface ISelectable
	{
		bool IsSelectable();
		bool Overlaps(Rect rectangle);
	}
}
