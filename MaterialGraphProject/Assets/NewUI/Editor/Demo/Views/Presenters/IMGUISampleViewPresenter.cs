using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	public class IMGUISampleElementPresenter : IMGUIPresenter
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

		protected IMGUISampleElementPresenter() {}
	}

	public class EditorGUISampleElementPresenter : IMGUIPresenter
	{
		private Color cc;
		private Color cc2;

		public override void OnGUIHandler()
		{
			cc =  EditorGUILayout.ColorField("Color", cc);
			cc2 =  EditorGUILayout.ColorField("Color2", cc2);
		}
	}

	public class IMGUISampleViewPresenter : GraphViewPresenter
	{
		protected new void OnEnable()
		{
			// Here, we would load the data from a given source and create our presenters based on that.
			// This is a demo, so our data (and thus our presenters) is fixed.

			base.OnEnable();
			var imguiSample = CreateInstance<IMGUISampleElementPresenter>();
			imguiSample.position = new Rect(100, 225, 230, 300);
			imguiSample.title = "IMGUIControls: modal";
			imguiSample.capabilities |= Capabilities.Resizable;
			AddElement(imguiSample);

			var imguiSample2 = CreateInstance<IMGUISampleElementPresenter>();
			imguiSample2.position = new Rect(400, 225, 230, 300);
			imguiSample2.title = "IMGUIControls: modal";
			imguiSample2.capabilities |= Capabilities.Resizable;
			AddElement(imguiSample2);

			var imguiEd = CreateInstance<EditorGUISampleElementPresenter>();
			imguiEd.position = new Rect(100, 25, 230, 75);
			imguiEd.title = "IMGUIControls: editor stuff";
			imguiEd.capabilities |= Capabilities.Resizable;
			AddElement(imguiEd);

			var imguiEd2 = CreateInstance<EditorGUISampleElementPresenter>();
			imguiEd2.position = new Rect(100, 125, 230, 75);
			imguiEd2.title = "IMGUIControls: editor stuff 2";
			imguiEd2.capabilities |= Capabilities.Resizable;
			AddElement(imguiEd2);
		}

		protected IMGUISampleViewPresenter() {}
	}
}
