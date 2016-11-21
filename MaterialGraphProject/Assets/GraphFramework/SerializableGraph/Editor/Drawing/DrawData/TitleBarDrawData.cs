using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class TitleBarDrawData : ScriptableObject
    {
        protected TitleBarDrawData()
        {
        }

		public string title;

		public void Initialize(IGraphAsset graphAsset)
		{
			if (graphAsset == null)
				title = "";
			else
				title = graphAsset.GetScriptableObject().name;
		}
    }
}