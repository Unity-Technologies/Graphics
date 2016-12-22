using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class ClickSelector : MouseManipulator
	{
		public ClickSelector()
		{
			// snoop events before children
			phaseInterest = EventPhase.Capture;
			activateButtons[(int)MouseButton.LeftMouse] = true;
			activateButtons[(int)MouseButton.RightMouse] = true;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var selectable = finalTarget.GetFirstOfType<ISelectable>();
			if (selectable == null || !selectable.IsSelectable())
			{
				return EventPropagation.Continue;
			}

			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (CanStartManipulation(evt))
					{
						if (graphView.selection.Contains(selectable))
						{
							if (evt.control)
							{
								graphView.RemoveFromSelection(selectable);
								return EventPropagation.Stop;
							}
							break;
						}

						var ve = selectable as VisualElement;
						if (ve != null && ve.parent == graphView.contentViewContainer)
						{
							if (!evt.control)
								graphView.ClearSelection();
							graphView.AddToSelection(selectable);
						}
					}
					break;
			}
			return EventPropagation.Continue;
		}
	}
}
