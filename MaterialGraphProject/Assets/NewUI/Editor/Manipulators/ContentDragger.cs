using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;

namespace RMGUI.GraphView
{
	// drags the contentContainer of a graphview around
	// add to the GraphView
	public class ContentDragger : Manipulator
	{
		private Vector2 m_Start;
		public Vector2 panSpeed { get; set; }

		public bool clampToParentEdges { get; set; }

		public ContentDragger()
		{
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
					if (evt.button == (int) MouseButton.MiddleMouse 
					|| evt.button == (int) MouseButton.LeftMouse && evt.modifiers == EventModifiers.Alt)
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

						graphView.contentViewContainer.transform = t*Matrix4x4.Translate(new Vector3(diff.x, diff.y, 0));
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
				if (this.HasCapture() 
					&& (evt.button == (int) MouseButton.MiddleMouse
						|| evt.button == (int) MouseButton.MiddleMouse))
					{
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}
	}
}
