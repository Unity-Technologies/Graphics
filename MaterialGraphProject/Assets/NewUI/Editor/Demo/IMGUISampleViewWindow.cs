using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class IMGUISampleViewWindow : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/IMGUISampleView")]
		public static void ShowWindow()
		{
			GetWindow<IMGUISampleViewWindow>();
		}

		void OnEnable()
		{
			var view = new SimpleContentView
			{
				name = "theView",
				dataSource = CreateInstance<IMGUISampleViewData>()
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
