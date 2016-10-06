using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;
using UnityEngine.VR.WSA.WebCam;

namespace RMGUI.GraphView
{

	// drags the contentContainer of a graphview around
	// add to the GraphView
	public class ContentDragger : Manipulator
	{
		private Vector2 m_Start;
		public Vector2 panSpeed { get; set; }

		public MouseButton activateButton { get; set; }

		public bool clampToParentEdges { get; set; }

		public ContentDragger()
		{
			activateButton = MouseButton.MiddleMouse;
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
			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button == (int) activateButton)
					{
						this.TakeCapture();

						m_Start = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, evt.mousePosition);

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture() && graphView.contentViewContainer.positionType == PositionType.Absolute)
					{
						var diff = graphView.ChangeCoordinatesTo(graphView.contentViewContainer, evt.mousePosition) - m_Start;
						var t = graphView.contentViewContainer.transform;

						graphView.contentViewContainer.transform = t * Matrix4x4.Translate(new Vector3(diff.x, diff.y, 0));
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (this.HasCapture() && evt.button == (int) activateButton)
					{
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}
	}

	public class Dragger : Manipulator
	{
		private Vector2 m_Start;

		public Vector2 panSpeed { get; set; }

		// hold the data... maybe.
		public GraphElementData m_data { get; set; }

		public MouseButton activateButton { get; set; }

		public bool clampToParentEdges { get; set; }

		public Dragger()
		{
			activateButton = MouseButton.MiddleMouse;
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
				var data = ce.GetData<GraphElementData>();
				if (data != null && ((data.capabilities & Capabilities.Movable) != Capabilities.Movable))
				{
					return EventPropagation.Continue;
				}
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button == (int) activateButton)
					{
						this.TakeCapture();

						var graphElement = target as GraphElement;
						if (graphElement != null)
						{
							m_data = graphElement.GetData<GraphElementData>();
						}

						m_Start = evt.mousePosition;

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture() && target.positionType == PositionType.Absolute)
					{
						var diff = evt.mousePosition - m_Start;

						if (m_data != null)
						{
							m_data.position = CalculatePosition(m_data.position.x + diff.x,
								m_data.position.y + diff.y,
								m_data.position.width, target.position.height);
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
					if (this.HasCapture() && evt.button == (int) activateButton)
					{
						m_data = null;
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
		}
	}
}
