using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	public class InvisibleBorderContainer : GraphElement
	{
		public override bool Overlaps(Rect rectangle)
		{
			return paddingRect.Overlaps(rectangle);
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			return paddingRect.Contains(localPoint);
		}

		public InvisibleBorderContainer()
		{
			elementTypeColor = new Color(0.0f, 1.0f, 0.0f, 0.5f);
		}
	}
}
