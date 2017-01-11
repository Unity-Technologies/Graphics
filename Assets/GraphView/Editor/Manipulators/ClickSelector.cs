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
			activators.Add(new ManipActivator {button = MouseButton.LeftMouse});
			activators.Add(new ManipActivator {button = MouseButton.RightMouse});
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var selectable = finalTarget.GetFirstOfType<ISelectable>();
			if (selectable == null || !selectable.IsSelectable())
			{
				return EventPropagation.Continue;
			}

			var graphView = target as GraphView;

            // thomasi : removed to be selectable anywhere
            /*if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}*/

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (CanStartManipulation(evt))
					{
						var ve = selectable as GraphElement;
                        if (ve != null)
                        {
                            return ve.Select(graphView, evt);
                        }
					}
					break;
			}
			return EventPropagation.Continue;
		}
	}
}
