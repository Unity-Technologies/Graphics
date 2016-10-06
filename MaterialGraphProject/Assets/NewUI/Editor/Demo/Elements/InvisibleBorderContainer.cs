using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(InvisibleBorderContainerData))]
	public class InvisibleBorderContainer : GraphElement
	{
		private readonly Color m_OutlineColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);

		private Rect selectRect
		{
			get
			{
				return new Rect(position.width * 0.1f, position.height * 0.1f, position.width * 0.8f, position.height * 0.8f);
			}
		}

		Rect GetRectWithOutline()
		{
			return new Rect(position.x + selectRect.x, position.y+selectRect.y, selectRect.width, selectRect.height);
		}

		public override bool Overlaps(Rect rectangle)
		{
			return GetRectWithOutline().Overlaps(rectangle);
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			return GetRectWithOutline().Contains(localPoint);
		}

		public override void DoRepaint(PaintContext args)
		{
			Color color = m_OutlineColor;
			if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
				color = Color.blue;
			Handles.DrawSolidRectangleWithOutline(position, color, color);

			Rect zone = GetRectWithOutline();
			Handles.DrawSolidRectangleWithOutline(zone, Color.green, Color.green);
		}
	}
}
