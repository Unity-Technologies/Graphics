using UnityEditor;

namespace RMGUI.GraphView.Demo
{
	public class SimpleGraphViewWindow : GraphViewEditorWindow
	{
		[MenuItem("Window/GraphView Demo/SimpleGraphView")]
		public static void ShowWindow()
		{
			GetWindow<SimpleGraphViewWindow>();
		}

		protected override GraphView BuildView()
		{
			return new SimpleContentView();
		}

		protected override GraphViewPresenter BuildPresenters()
		{
			return CreateInstance<SimpleContentViewPresenter>();
		}
	}
}
