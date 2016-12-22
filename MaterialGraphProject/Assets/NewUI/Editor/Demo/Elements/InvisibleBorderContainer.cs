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
	}
}
