using System.Collections;
using System.Linq;
using NUnit.Framework;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

[UnityPlatform(TestPlatform.EditMode)]
public class GraphElementMoveTests
{
	class TestViewPresenter : GraphViewPresenter
	{
		protected new void OnEnable()
		{
			base.OnEnable();

			var movableElementPresenter = CreateInstance<SimpleElementPresenter>();
			movableElementPresenter.position = new Rect(10, 30, 50, 50);
			movableElementPresenter.title = "Movable element";
			AddElement(movableElementPresenter);

			var miniMapPresenter = CreateInstance<MiniMapPresenter>();
			miniMapPresenter.position = new Rect(100, 100, 100, 100);
			miniMapPresenter.maxWidth = 100;
			miniMapPresenter.maxHeight = 100;
			AddElement(miniMapPresenter);
		}

		protected TestViewPresenter() { }
	}

	class TestViewWindow : GraphViewEditorWindow
	{
		public static void ShowWindow()
		{
			GetWindow<TestViewWindow>();
		}

		protected override GraphView BuildView()
		{
			return new SimpleContentView();
		}

		protected override GraphViewPresenter BuildPresenters()
		{
			return CreateInstance<TestViewPresenter>();
		}
	}

	[UnityTest]
	public IEnumerator MovableElementCanBeDragged()
	{
		TestViewWindow window = EditorWindow.GetWindow<TestViewWindow>();

		// Move the movable element.
		window.SendEvent(new Event {type = EventType.MouseDown, mousePosition = new Vector2(15, 50)});
		yield return null;

		window.SendEvent(new Event {type = EventType.MouseDrag, delta = new Vector2(10, -10)});
		yield return null;

		window.SendEvent(new Event {type = EventType.MouseUp, mousePosition = new Vector2(25, 40)});
		yield return null;

		var winPres = window.GetPresenter<TestViewPresenter>();
		var elemPres = winPres.elements.OfType<SimpleElementPresenter>().First();

		Assert.AreEqual(20, elemPres.position.x);
		Assert.AreEqual(20, elemPres.position.y);

		window.Close();
		yield return null;
	}

	[UnityTest]
	public IEnumerator MiniMapElementCanBeDragged()
	{
		TestViewWindow window = EditorWindow.GetWindow<TestViewWindow>();

		// Move the minimap element.
		window.SendEvent(new Event {type = EventType.MouseDown, mousePosition = new Vector2(110, 130)});
		yield return null;

		window.SendEvent(new Event {type = EventType.MouseDrag, mousePosition = new Vector2(120, 120)});
		yield return null;

		window.SendEvent(new Event {type = EventType.MouseUp, mousePosition = new Vector2(120, 120)});
		yield return null;

		var winPres = window.GetPresenter<TestViewPresenter>();
		var miniMapPres = winPres.elements.OfType<MiniMapPresenter>().First();

		Assert.AreEqual(110, miniMapPres.position.x);
		Assert.AreEqual(90, miniMapPres.position.y);

		window.Close();
		yield return null;
	}
}
