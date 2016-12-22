using System;

namespace RMGUI.GraphView
{
	[Serializable]
	public class IMGUIPresenter : SimpleElementPresenter
	{
		protected new void OnEnable()
		{
			base.OnEnable();
			title = "BaseIMGUI";
		}

		public virtual void OnGUIHandler()
		{
		}

		protected IMGUIPresenter() { }
	}
}
