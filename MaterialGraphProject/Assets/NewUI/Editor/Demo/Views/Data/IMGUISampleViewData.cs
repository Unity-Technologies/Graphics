using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(IMGUIElement))]
	public class IMGUISampleElementData : IMGUIData
	{
		private int m_ControlInteger;
		private bool m_GUIToggle;
		private float m_GUIFloatValue = 42.0f;
		private string m_SomeText = "<enter some text>";

		public override void OnGUIHandler()
		{
			Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);

			Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
			EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), selected ? selectedColor : backgroundColor);

			GUILayout.BeginVertical();
			GUILayout.Label("Layout begins here");
			GUILayout.Label("X = " + position.x + " Y = " + position.y + " W = " + position.width + " H = " + position.height);
			if (GUILayout.Button("Layout Buttton" + m_ControlInteger, GUILayout.Width(150)))
			{
				m_ControlInteger++;
				Debug.Log("Layout Button was pressed: " + m_ControlInteger);
			}

			m_GUIToggle = GUILayout.Toggle(m_GUIToggle, "GUI Toggle");

			m_GUIFloatValue = GUILayout.HorizontalSlider(m_GUIFloatValue, 0.0f, 100.0f, GUILayout.Width(150));

			m_SomeText = GUILayout.TextField(m_SomeText, GUILayout.Width(200));
			GUILayout.Label("Layout ends here");
			GUILayout.EndVertical();

			float y = 150.0f;
			GUI.Label(new Rect(0, y, 150, 30), "No-layout begins here");

			if (GUI.Button(new Rect(0, y + 30.0f, 120, 30), "GUI Button:" + m_ControlInteger))
			{
				m_ControlInteger++;
				Debug.Log("GUI Button was pressed: " + m_ControlInteger);
			}

			m_GUIToggle = GUI.Toggle(new Rect(120, y + 30.0f, 120, 30), m_GUIToggle, "GUI Toggle");

			m_GUIFloatValue = GUI.HorizontalSlider(new Rect(0, y + 60.0f, 120, 30), m_GUIFloatValue, 0.0f, 100.0f);

			GUI.Label(new Rect(0, y + 90.0f, 120, 30), "No-layout ends here");
		}
	}

	public class IMGUISampleViewData : GraphViewDataSource
	{
		protected void OnEnable()
		{
			var imguiSample = CreateInstance<IMGUISampleElementData>();
			imguiSample.position = new Rect(100, 200, 230, 300);
			imguiSample.title = "IMGUIControls: modal";
			imguiSample.capabilities |= Capabilities.Resizable;
			AddElement(imguiSample);
		}
	}
}
