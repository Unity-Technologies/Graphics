using UnityEngine;

namespace RMGUI.GraphView
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
			var elementPresenter = GetPresenter<SimpleElementPresenter>();
			content.text = elementPresenter.title;
		}
	}
}
