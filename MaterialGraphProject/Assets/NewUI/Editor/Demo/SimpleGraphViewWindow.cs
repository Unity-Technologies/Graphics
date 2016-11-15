using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class SimpleGraphViewWindow : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/SimpleGraphView")]
		public static void ShowWindow()
		{
			GetWindow<SimpleGraphViewWindow>();
		}

		void OnEnable()
		{
			var view = new SimpleContentView
			{
				name = "theView",
				dataSource = CreateInstance<SimpleContentViewData>()
			};
			view.StretchToParentSize();
            rootVisualContainer.AddChild(view);
		}

		void OnDisable()
		{
            rootVisualContainer.ClearChildren();
		}
	}
}
