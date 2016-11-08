using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class SimpleElement : GraphElement
	{
		public SimpleElement()
		{
			content = new GUIContent("");
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			var elementData = GetData<SimpleElementData>();
			content.text = elementData.title;
		}
	}
}
