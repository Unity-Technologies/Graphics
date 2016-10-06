using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class ClickSelector : Manipulator
	{
		public MouseButton activateButton { get; set; }

		public ClickSelector()
		{
			// snoop events before children
			phaseInterest = EventPhase.Capture;
			activateButton = MouseButton.LeftMouse;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			ISelectable selectable = finalTarget.GetFirstOfType<ISelectable>();
			if ( selectable==null || !selectable.IsSelectable())
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
					if (evt.button == (int)activateButton)
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

						VisualElement ve = selectable as VisualElement;
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
