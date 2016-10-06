using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class SimpleGraphView : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/SimpleGraphView")]
		public static void ShowWindow()
		{
			GetWindow<SimpleGraphView>();
		}

		void OnEnable()
		{
			var zeView = new SimpleContentView
			{
				name = "theView",
				dataProvider = CreateInstance<SimpleGraphViewData>()
			};
			zeView.StretchToParentSize();

			windowRoot.AddChild(zeView);
		}

		void OnDisable()
		{
			windowRoot.ClearChildren();
		}
	}
}
