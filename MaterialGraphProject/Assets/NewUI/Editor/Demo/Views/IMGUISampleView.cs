using UnityEditor;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class IMGUISampleView : EditorWindow
	{
		[MenuItem("Window/GraphView Demo/IMGUISampleView")]
		public static void ShowWindow()
		{
			GetWindow<IMGUISampleView>();
		}

		void OnEnable()
		{
			var view = new SimpleContentView
			{
				name = "theView",
				dataProvider = CreateInstance<IMGUISampleViewData>()
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
