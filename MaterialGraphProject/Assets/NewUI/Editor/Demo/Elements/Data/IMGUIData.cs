using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class IMGUIData : SimpleElementData
	{
		public IMGUIData()
		{
			title = "BaseIMGUI";
		}

		public virtual void OnGUIHandler()
		{
		}
	}
}
