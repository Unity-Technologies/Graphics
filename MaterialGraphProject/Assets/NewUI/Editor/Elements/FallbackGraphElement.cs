using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	[GUISkinStyle("box")]
	class FallbackGraphElement : GraphElement
	{
		public FallbackGraphElement()
		{
			content = new GUIContent("");
		}

		public override void OnDataChanged()
		{
			var data = GetData<GraphElementData>();
			content.text = "Fallback for " + data.GetType() + ". No GraphElement registered for this type in this view.";
		}
	}
}
