using UnityEditor;

namespace RMGUI.GraphView.Demo
{
	class NodalViewWindow : GraphViewEditorWindow
	{
		[MenuItem("Window/GraphView Demo/Nodal UI")]
		public static void ShowWindow()
		{
			GetWindow<NodalViewWindow>();
		}

		protected override GraphView BuildView()
		{
			return new NodesContentView();
		}

		protected override GraphViewPresenter BuildPresenters()
		{
			return CreateInstance<NodesContentViewPresenter>();
		}
	}
}
