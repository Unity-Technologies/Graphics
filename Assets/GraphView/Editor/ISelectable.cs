using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public interface ISelectable
	{
		bool IsSelectable();
		bool Overlaps(Rect rectangle);

        //thomasi : added handling of selection
        EventPropagation Select(VisualContainer selectionContainer, Event evt);
	}
}
