using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class IMGUIData : SimpleElementData
	{
		protected new void OnEnable()
		{
			base.OnEnable();
			title = "BaseIMGUI";
		}

		public virtual void OnGUIHandler()
		{
		}

		protected IMGUIData() { }
	}
}
