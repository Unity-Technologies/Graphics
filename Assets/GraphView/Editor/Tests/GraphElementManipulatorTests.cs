using System.Collections;
using System.Linq;
using NUnit.Framework;
using RMGUI.GraphView;
using RMGUI.GraphView.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.TestTools;

[UnityPlatform(TestPlatform.EditMode)]
public class GraphElementManipulatorTests
{
	class TestViewPresenter : GraphViewPresenter
	{
		protected new void OnEnable()
		{
			base.OnEnable();

			var deletableElementPresenter = CreateInstance<SimpleElementPresenter>();
			deletableElementPresenter.capabilities |= Capabilities.Deletable;
			deletableElementPresenter.position = new Rect(0, 0, 50, 50);
			deletableElementPresenter.title = "Deletable element";
			AddElement(deletableElementPresenter);

			var undeletableElementPresenter = CreateInstance<SimpleElementPresenter>();
			undeletableElementPresenter.capabilities &= ~Capabilities.Deletable;
			undeletableElementPresenter.position = new Rect(50, 0, 50, 50);
			undeletableElementPresenter.title = "Deletable element";
			AddElement(undeletableElementPresenter);
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
	public IEnumerator DeletableElementCanBeDeleted()
	{
		TestViewWindow window = EditorWindow.GetWindow<TestViewWindow>();

		var winPres = window.GetPresenter<TestViewPresenter>();
		var testMousePosition = new Vector2(15, 30);

		Assert.AreEqual(2, winPres.elements.Count());

		// Click-select the deletable element.
		window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = testMousePosition });
		yield return null;

		window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = testMousePosition });
		yield return null;

		// Delete it using the Delete hotkey
		window.SendEvent(new Event { type = EventType.KeyDown, mousePosition = testMousePosition, keyCode = KeyCode.Delete, modifiers = EventModifiers.FunctionKey });
		yield return null;

		Assert.AreEqual(1, winPres.elements.Count());

		// Click-select the undeletable element.
		testMousePosition = new Vector2(65, 30);

		window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = testMousePosition });
		yield return null;

		window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = testMousePosition });
		yield return null;

		// Delete it using the Delete hotkey
		window.SendEvent(new Event { type = EventType.KeyDown, mousePosition = testMousePosition, keyCode = KeyCode.Delete, modifiers = EventModifiers.FunctionKey });
		yield return null;

		Assert.AreEqual(1, winPres.elements.Count());

		window.Close();
		yield return null;
	}
}
