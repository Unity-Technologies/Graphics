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
            rootVisualContainer.AddChild(view);
		}

		void OnDisable()
		{
            rootVisualContainer.ClearChildren();
		}
	}
}
