using UnityEditor;

namespace RMGUI.GraphView.Demo
{
	public class IMGUISampleViewWindow : GraphViewEditorWindow
	{
		[MenuItem("Window/GraphView Demo/IMGUISampleView")]
		public static void ShowWindow()
		{
			GetWindow<IMGUISampleViewWindow>();
		}

		protected override GraphView BuildView()
		{
			return new SimpleContentView();
		}

		protected override GraphViewPresenter BuildPresenters()
		{
			return CreateInstance<IMGUISampleViewPresenter>();
		}
	}
}
