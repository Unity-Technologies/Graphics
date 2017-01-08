using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	public class Circle : GraphElement
	{
		public override void DoRepaint(IStylePainter painter)
		{
			Handles.DrawSolidDisc(new Vector3(position.x + position.width/2, position.y + position.height/2, 0.0f),
								  new Vector3(0.0f, 0.0f, -1.0f),
								  position.width / 2.0f);

			var circlePresenter = GetPresenter<CirclePresenter>();
			if (!circlePresenter.selected)
				return;

			Color oldColor = Handles.color;
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(new Vector3(position.x + position.width/2, position.y + position.height/2, 0.0f),
				new Vector3(0.0f, 0.0f, -1.0f),
				position.width/2.0f+2.0f);
			Handles.color = oldColor;
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			return Vector2.Distance(new Vector2(position.width/2, position.height/2), localPoint-position.position) <= position.width / 2.0f;
		}

		public override bool Overlaps(Rect rectangle)
		{
			rectangle.position -= position.position;
			var radius = position.width / 2.0f;
			var p = new Vector2(Mathf.Max(rectangle.x, Mathf.Min(radius, rectangle.xMax)), Mathf.Max(rectangle.y, Mathf.Min(radius, rectangle.yMax)));
			return Vector2.Distance(new Vector2(radius, radius), p) <= radius;
		}

		public Circle()
		{
			elementTypeColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
		}
	}
}
