using System;
using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[Serializable]
	public class TestIMGUIElementPresenter : IMGUIPresenter
	{
		public string m_Text1 = "this is a text field";
		public string m_Text2 = "this is a text field";
		public bool m_Toggle = true;
		public Texture2D m_Texture;

		public override void OnGUIHandler()
		{
			int currentY = 22;

			m_Text1 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text1);
			currentY += 22;

			m_Toggle = GUI.Toggle(new Rect(0, currentY, 10, 10), m_Toggle, GUIContent.none);
			currentY += 22;

			m_Text2 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text2);
			currentY += 22;

			m_Texture = EditorGUI.ObjectField(new Rect(0, currentY, 80, 100), m_Texture, typeof(Texture2D), false) as Texture2D;
		}

		protected TestIMGUIElementPresenter() {}
	}

	public class SimpleContentViewPresenter : GraphViewPresenter
	{
		protected new void OnEnable()
		{
			base.OnEnable();

			// Here, we would load the data from a given source and create our presenters based on that.
			// This is a demo, so our data (and thus our presenters) is fixed.

			var simpleElementPresenter = CreateInstance<SimpleElementPresenter>();
			simpleElementPresenter.position = new Rect(0, 0, 200, 200);
			simpleElementPresenter.title = "Static element";
			simpleElementPresenter.capabilities &= ~Capabilities.Movable; // Make this simple element non-movable
			AddElement(simpleElementPresenter);

			var resizableElementPresenter = CreateInstance<SimpleElementPresenter>();
			resizableElementPresenter.position = new Rect(400, 100, 100, 100);
			resizableElementPresenter.title = "Resizable element";
			resizableElementPresenter.capabilities |= Capabilities.Resizable;
			AddElement(resizableElementPresenter);

			var imguiSamplePresenter = CreateInstance<TestIMGUIElementPresenter>();
			imguiSamplePresenter.position = new Rect(100, 200, 100, 100);
			imguiSamplePresenter.title = "IMGUI sample";
			imguiSamplePresenter.capabilities |= Capabilities.Resizable;
			AddElement(imguiSamplePresenter);

			var movableElementPresenter = CreateInstance<SimpleElementPresenter>();
			movableElementPresenter.position = new Rect(400, 400, 200, 200);
			movableElementPresenter.title = "Movable element";
			AddElement(movableElementPresenter);

			var circlePresenter = CreateInstance<CirclePresenter>();
			circlePresenter.position = new Rect(200, 500, 0, 0);
			circlePresenter.radius = 100;
			AddElement(circlePresenter);

			var wwwImagePresenter = CreateInstance<WWWImagePresenter>();
			wwwImagePresenter.title = "WWW Image";
			wwwImagePresenter.position = new Rect(300, 300, 204, 225);
			AddElement(wwwImagePresenter);

			var invisibleBorderContainerPresenter = CreateInstance<InvisibleBorderContainerPresenter>();
			invisibleBorderContainerPresenter.position = new Rect(400, 0, 100, 100);
			AddElement(invisibleBorderContainerPresenter);

			var miniMapPresenter = CreateInstance<MiniMapPresenter>();
			miniMapPresenter.position = new Rect(0, 500, 100, 100);
			AddElement(miniMapPresenter);
		}

		protected SimpleContentViewPresenter() {}
	}
}
