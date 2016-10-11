using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	[CustomDataView(typeof(IMGUIElement))]
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
