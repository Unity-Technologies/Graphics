using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class WWWImageData : SimpleElementData
	{
		protected new void OnEnable()
		{
			base.OnEnable();
			title = "WWWImage";
		}

		protected WWWImageData() {}
	}
}
