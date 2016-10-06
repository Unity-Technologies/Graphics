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
			var zeView = new SimpleContentView
			{
				name = "theView",
				dataProvider = CreateInstance<IMGUISampleViewData>()
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
