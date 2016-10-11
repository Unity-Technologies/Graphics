using System;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	[CustomDataView(typeof(WWWImage))]
	public class WWWImageData : SimpleElementData
	{
		public WWWImageData()
		{
			title = "WWWImage";
		}
	}
}
