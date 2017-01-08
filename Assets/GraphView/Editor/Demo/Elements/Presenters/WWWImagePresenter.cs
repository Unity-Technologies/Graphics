using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class WWWImagePresenter : SimpleElementPresenter
	{
		protected new void OnEnable()
		{
			base.OnEnable();
			title = "WWWImage";
		}

		protected WWWImagePresenter() {}
	}
}
