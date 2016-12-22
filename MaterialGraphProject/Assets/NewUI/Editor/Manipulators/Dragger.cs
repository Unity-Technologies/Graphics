using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;

namespace RMGUI.GraphView
{
	public class Dragger : MouseManipulator
	{
		private Vector2 m_Start;

		public Vector2 panSpeed { get; set; }

 		// hold the presenter... maybe.
 		public GraphElementPresenter m_presenter { get; set; }

		public bool clampToParentEdges { get; set; }

		public Dragger()
		{
			activateButtons[(int)MouseButton.LeftMouse] = true;
			panSpeed = new Vector2(1, 1);
			clampToParentEdges = false;
		}

		protected Rect CalculatePosition(float x, float y, float width, float height)
		{
			var rect = new Rect(x, y, width, height);

			if (clampToParentEdges)
			{
				if (rect.x < target.parent.position.xMin)
					rect.x = target.parent.position.xMin;
				else if (rect.xMax > target.parent.position.xMax)
					rect.x = target.parent.position.xMax - rect.width;

				if (rect.y < target.parent.position.yMin)
					rect.y = target.parent.position.yMin;
				else if (rect.yMax > target.parent.position.yMax)
					rect.y = target.parent.position.yMax - rect.height;

				// Reset size, we never intended to change them in the first place
				rect.width = width;
				rect.height = height;
			}

			return rect;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			GraphElement ce = finalTarget as GraphElement;
			if (ce != null)
			{
				GraphElementPresenter presenter = ce.presenter;
				if (presenter != null && ((presenter.capabilities & Capabilities.Movable) != Capabilities.Movable))
				{
					return EventPropagation.Continue;
				}
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (CanStartManipulation(evt))
					{
						this.TakeCapture();

						var graphElement = target as GraphElement;
						if (graphElement != null)
						{
							m_presenter = graphElement.presenter;
						}

						m_Start = evt.mousePosition;

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture() && target.positionType == PositionType.Absolute)
					{
						Vector2 diff = evt.mousePosition - m_Start;

						if (m_presenter != null)
						{
							m_presenter.position = CalculatePosition(m_presenter.position.x + diff.x,
																	 m_presenter.position.y + diff.y,
																	 m_presenter.position.width, target.position.height);
						}
						else
						{
							target.position = CalculatePosition(target.position.x + diff.x,
																target.position.y + diff.y,
																target.position.width, target.position.height);
						}

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (CanStopManipulation(evt))
					{
						m_presenter = null;
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
		}
	}
}
