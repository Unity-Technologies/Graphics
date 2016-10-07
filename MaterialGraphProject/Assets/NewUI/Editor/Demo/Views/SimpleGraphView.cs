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
			var view = new SimpleContentView
			{
				name = "theView",
				dataProvider = CreateInstance<SimpleGraphViewData>()
			};
			view.StretchToParentSize();

			windowRoot.AddChild(view);
		}

		void OnDisable()
		{
			windowRoot.ClearChildren();
		}
	}
}
