using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[GUISkinStyle("window")]
	public class SimpleElement : GraphElement
	{
		public SimpleElement()
		{
			content = new GUIContent("");
		}

		public override void DoRepaint(PaintContext painter)
		{
			base.DoRepaint(painter);
			if (GetData<SimpleElementData>() != null && GetData<SimpleElementData>().selected)
			{
				painter.DrawRectangleOutline(transform, position, Color.yellow);
			}
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			var elementData = GetData<SimpleElementData>();
			content.text = elementData.title;
		}
	}
}
