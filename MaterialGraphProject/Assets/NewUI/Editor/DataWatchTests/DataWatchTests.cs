using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.PlaymodeTests;
using UnityEngine.Assertions;
using UnityEditor;
using System.Collections;

[EditModeTest]
public class DataWatchTests
{
	class MyWindow : EditorWindow
	{
	}

	class MyData : ScriptableObject
	{
		public int myField;
	}

	int m_Calls;
	MyData data;
	DataWatchService watch;

	public DataWatchTests()
	{
		data = ScriptableObject.CreateInstance<MyData>();
		watch = new DataWatchService();
	}

	void OnDataChanged()
	{
		m_Calls++;
	}

	[EditModeTest]
	public IEnumerator DataWatchCallsOnChanged()
	{
		var window = EditorWindow.GetWindow<MyWindow>();

		IDataWatchHandle handle = watch.AddWatch(window.rootVisualContainer, data, OnDataChanged);
		Assert.IsTrue(watch.IsActive(handle), "Expected watch to be active");

		data.myField = 1;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 1, "Expected first call after change");

		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 1, "Expected no new call after no change");

		data.myField = 1;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 1, "Expected no new call after idempotent change");

		data.myField = 2;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 2, "Expect second callback after change");

		handle.Dispose();
		window.Close();
		yield return null;
	}

	[EditModeTest]
	public IEnumerator DataWatchCallsOnChangedForDestroyedObject() {

		var window = EditorWindow.GetWindow<MyWindow>();

		yield return null;

		IDataWatchHandle handle = watch.AddWatch(window.rootVisualContainer, data, OnDataChanged);

		ScriptableObject.DestroyImmediate(data);
		Assert.IsTrue(watch.IsActive(handle), "Expected watch to still be active after destruction");
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 1, "Expect a call after destruction");

		window.Close();
		yield return null;
	}

	[EditModeTest]
	public IEnumerator CanStopWatchingObject()
	{
		var window = EditorWindow.GetWindow<MyWindow>();

		IDataWatchHandle handle = watch.AddWatch(window.rootVisualContainer, data, OnDataChanged);

		handle.Dispose();
		Assert.IsFalse(watch.IsActive(handle), "Expected watch to not be active after removal");

		data.myField = 1;

		Assert.AreEqual(m_Calls, 0, "Expected no call after removing watch");

		window.Close();
		yield return null;
	}

	[EditModeTest]
	public IEnumerator CanStopWatchingObjectDuringCallback()
	{
		IDataWatchHandle handle = null;
		var window = EditorWindow.GetWindow<MyWindow>();

		Action removeOnCall = () => handle.Dispose();

		handle = watch.AddWatch(window.rootVisualContainer, data, removeOnCall);

		data.myField = 1;
		watch.ProcessNotificationQueue();

		Assert.IsFalse(watch.IsActive(handle), "Expected watch to not be active after removal");

		window.Close();
		yield return null;
	}

	[EditModeTest]
	public IEnumerator ReceivesCallbackForUndo()
	{
		var window = EditorWindow.GetWindow<MyWindow>();
		IDataWatchHandle handle = watch.AddWatch(window.rootVisualContainer, data, OnDataChanged);

		Undo.RecordObject(data, "change");
		data.myField = 1;
		yield return null;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 1, "Expected first call after change");

		Undo.PerformUndo();
		yield return null;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 2, "Expected second call after undo");

		Undo.PerformRedo();
		yield return null;
		watch.ProcessNotificationQueue();
		Assert.AreEqual(m_Calls, 3, "Expected third call after redo");

		handle.Dispose();
		window.Close();
		yield return null;
	}
}
