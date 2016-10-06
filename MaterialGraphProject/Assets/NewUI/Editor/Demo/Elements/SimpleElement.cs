using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[GUISkinStyle("window")]
	[CustomDataView(typeof(SimpleElementData))]
	public class SimpleElement : GraphElement
	{
		public SimpleElement()
		{
			content = new GUIContent("");
		}

		public override void DoRepaint(PaintContext painter)
		{
			base.DoRepaint(painter);
			if (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected)
			{
				painter.DrawRectangleOutline(transform, position, Color.yellow);
			}
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			var elementData = (SimpleElementData)dataProvider;
			content.text = elementData.title;
		}
	}
}
