using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	class NodalViewWindow : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/Nodal UI")]
		public static void ShowWindow()
		{
			GetWindow<NodalViewWindow>();
		}

		void OnEnable()
		{
			var view = new NodesContentView
			{
				name = "theView",
				dataSource = CreateInstance<NodesContentViewData>()
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
