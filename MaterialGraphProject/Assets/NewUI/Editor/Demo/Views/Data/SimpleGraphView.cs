using System;
using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(IMGUIElement))]
	[Serializable]
	public class TestIMGUIElementData : IMGUIData
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
	}

	public class SimpleGraphViewData : GraphViewDataSource
	{
		protected void OnEnable()
		{
			var simpleElementData = CreateInstance<SimpleElementData>();
			simpleElementData.position = new Rect(0, 0, 200, 200);
			simpleElementData.title = "Static element";
			simpleElementData.capabilities &= ~Capabilities.Movable; // Make this simple element non-movable
			AddElement(simpleElementData);

			var resizableElementData = CreateInstance<SimpleElementData>();
			resizableElementData.position = new Rect(400, 100, 100, 100);
			resizableElementData.title = "Resizable element";
			resizableElementData.capabilities |= Capabilities.Resizable;
			AddElement(resizableElementData);

			var imguiSampleData = CreateInstance<TestIMGUIElementData>();
			imguiSampleData.position = new Rect(100, 200, 100, 100);
			imguiSampleData.title = "IMGUI sample";
			imguiSampleData.capabilities |= Capabilities.Resizable;
			AddElement(imguiSampleData);

			var movableElementData = CreateInstance<SimpleElementData>();
			movableElementData.position = new Rect(400, 400, 200, 200);
			movableElementData.title = "Movable element";
			AddElement(movableElementData);

			var miniMapData = CreateInstance<MiniMapData>();
			miniMapData.position = new Rect(0, 500, 100, 100);
			AddElement(miniMapData);

			var circleData = CreateInstance<CircleData>();
			circleData.position = new Rect(200, 500, 0, 0);
			circleData.radius = 100;
			AddElement(circleData);

			var wwwImageData = CreateInstance<WWWImageData>();
			wwwImageData.title = "WWW Image";
			wwwImageData.position = new Rect(300, 300, 200, 200);
			AddElement(wwwImageData);

			var invisibleBorderContainerData = CreateInstance<InvisibleBorderContainerData>();
			invisibleBorderContainerData.position = new Rect(400, 0, 100, 100);
			AddElement(invisibleBorderContainerData);
		}
	}
}
