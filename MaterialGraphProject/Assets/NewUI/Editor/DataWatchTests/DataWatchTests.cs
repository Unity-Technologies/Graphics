using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEditor;
using System.Collections;

[UnityPlatform(TestPlatform.EditMode)]
public class DataWatchTests
{
	class MyWindow : EditorWindow
	{
		public int calls;
		public DataWatchService watch;

		public MyWindow()
		{
			watch = new DataWatchService();
		}

		public void OnDataChanged()
		{
			calls++;
		}
	}

	class MyData : ScriptableObject
	{
		public int myField;
	}

	[UnityTest]
	public IEnumerator DataWatchCallsOnChanged()
	{
		var data = ScriptableObject.CreateInstance<MyData>();

		var window = EditorWindow.GetWindow<MyWindow>();

		IDataWatchHandle handle = window.watch.AddWatch(window.rootVisualContainer, data, window.OnDataChanged);
		Assert.IsTrue(window.watch.IsActive(handle), "Expected watch to be active");

		data.myField = 1;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 1, "Expected first call after change");

		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 1, "Expected no new call after no change");

		data.myField = 1;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 1, "Expected no new call after idempotent change");

		data.myField = 2;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 2, "Expect second callback after change");

		handle.Dispose();
		window.Close();
		yield return null;
	}

	[UnityTest]
	public IEnumerator DataWatchCallsOnChangedForDestroyedObject() {

		var window = EditorWindow.GetWindow<MyWindow>();

		yield return null;

		var data = ScriptableObject.CreateInstance<MyData>();

		IDataWatchHandle handle = window.watch.AddWatch(window.rootVisualContainer, data, window.OnDataChanged);

		ScriptableObject.DestroyImmediate(data);
		Assert.IsTrue(window.watch.IsActive(handle), "Expected watch to still be active after destruction");
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 1, "Expect a call after destruction");

		window.Close();
		yield return null;
	}

	[UnityTest]
	public IEnumerator CanStopWatchingObject()
	{
		var data = ScriptableObject.CreateInstance<MyData>();
		var window = EditorWindow.GetWindow<MyWindow>();

		IDataWatchHandle handle = window.watch.AddWatch(window.rootVisualContainer, data, window.OnDataChanged);

		handle.Dispose();
		Assert.IsFalse(window.watch.IsActive(handle), "Expected watch to not be active after removal");

		data.myField = 1;

		Assert.AreEqual(window.calls, 0, "Expected no call after removing watch");

		window.Close();
		yield return null;
	}

	[UnityTest]
	public IEnumerator CanStopWatchingObjectDuringCallback()
	{
		var data = ScriptableObject.CreateInstance<MyData>();

		IDataWatchHandle handle = null;
		var window = EditorWindow.GetWindow<MyWindow>();

		Action removeOnCall = () => handle.Dispose();

		handle = window.watch.AddWatch(window.rootVisualContainer, data, removeOnCall);

		data.myField = 1;
		window.watch.ProcessNotificationQueue();

		Assert.IsFalse(window.watch.IsActive(handle), "Expected watch to not be active after removal");

		window.Close();
		yield return null;
	}

	[UnityTest]
	public IEnumerator ReceivesCallbackForUndo()
	{
		var data = ScriptableObject.CreateInstance<MyData>();

		var window = EditorWindow.GetWindow<MyWindow>();
		IDataWatchHandle handle = window.watch.AddWatch(window.rootVisualContainer, data, window.OnDataChanged);

		Undo.RecordObject(data, "change");
		data.myField = 1;
		yield return null;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 1, "Expected first call after change");

		Undo.PerformUndo();
		yield return null;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 2, "Expected second call after undo");

		Undo.PerformRedo();
		yield return null;
		window.watch.ProcessNotificationQueue();
		Assert.AreEqual(window.calls, 3, "Expected third call after redo");

		handle.Dispose();
		window.Close();
		yield return null;
	}
}
