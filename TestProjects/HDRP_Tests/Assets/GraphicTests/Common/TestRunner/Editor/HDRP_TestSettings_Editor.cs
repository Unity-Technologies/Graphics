using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(HDRP_TestSettings)), DisallowMultipleComponent]
public class HDRP_TestSettings_Editor : Editor
{
    HDRP_TestSettings typedTarget;
    bool liveViewSize = false;
    int prevWidth = 0;
    int prevHeight = 0;

    void OnEnable()
    {
        typedTarget = target as HDRP_TestSettings;
    }

    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();

        liveViewSize = GUILayout.Toggle(liveViewSize, "Auto update game view.");

        if ( GUILayout.Button( "Set Game View Size") || liveViewSize && ( prevWidth != typedTarget.ImageComparisonSettings.TargetWidth || prevHeight != typedTarget.ImageComparisonSettings.TargetHeight ) )
        {
            prevWidth = typedTarget.ImageComparisonSettings.TargetWidth;
            prevHeight = typedTarget.ImageComparisonSettings.TargetHeight;

            SetGameViewSize();
        }

		if (GUILayout.Button("Fix Texts"))
		{
			TextMeshPixelSize[] texts = FindObjectsOfType<TextMeshPixelSize>();
			foreach( TextMeshPixelSize text in texts )
			{
				text.CorrectPosition();
			}

			UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
		}
    }

    static string testGameViewSize = "Graphic Test";
    void SetGameViewSize()
	{
        if (typedTarget == null) return;

		if ( GameViewUtils.SizeExists( GameViewSizeGroupType.Standalone, testGameViewSize) )
			GameViewUtils.RemoveCustomSize( GameViewSizeGroupType.Standalone, GameViewUtils.FindSize( GameViewSizeGroupType.Standalone, testGameViewSize )  );

		GameViewUtils.AddCustomSize( GameViewUtils.GameViewSizeType.FixedResolution, GameViewSizeGroupType.Standalone, typedTarget.ImageComparisonSettings.TargetWidth, typedTarget.ImageComparisonSettings.TargetHeight, testGameViewSize );

		GameViewUtils.SetSize( GameViewUtils.FindSize( GameViewSizeGroupType.Standalone, testGameViewSize ) );
	}

    // Set game view size. Code comes from : https://answers.unity.com/questions/956123/add-and-select-game-view-resolution.html
	public static class GameViewUtils
	{
		static object gameViewSizesInstance;
		static MethodInfo getGroup;

		static GameViewUtils()
		{
			// gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
			var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
			var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
			var instanceProp = singleType.GetProperty("instance");
			getGroup = sizesType.GetMethod("GetGroup");
			gameViewSizesInstance = instanceProp.GetValue(null, null);
		}

		public enum GameViewSizeType
		{
			AspectRatio, FixedResolution
		}

		public static void SetSize(int index)
		{
			var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
			var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var gvWnd = EditorWindow.GetWindow(gvWndType);
			selectedSizeIndexProp.SetValue(gvWnd, index, null);
		}

		public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
		{
			// GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupTyge);
			// group.AddCustomSize(new GameViewSize(viewSizeType, width, height, text);

			var group = GetGroup(sizeGroupType);
			var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
			var gvsType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
		    Type gvstType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
		    var ctor = gvsType.GetConstructor(new System.Type[] { gvstType, typeof(int), typeof(int), typeof(string) });
			var newSize = ctor.Invoke(new object[] { (int)viewSizeType , width, height, text });
			addCustomSize.Invoke(group, new object[] { newSize });
		}

		public static void RemoveCustomSize(GameViewSizeGroupType sizeGroupType, int index)
		{
			var group = GetGroup(sizeGroupType);
			var removeCutomSize = group.GetType().GetMethod("RemoveCustomSize");

			removeCutomSize.Invoke(group, new object[] { index });
		}

		public static bool SizeExists(GameViewSizeGroupType sizeGroupType, string text)
		{
			return FindSize(sizeGroupType, text) != -1;
		}

		public static int FindSize(GameViewSizeGroupType sizeGroupType, string text)
		{
			// GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
			// string[] texts = group.GetDisplayTexts();
			// for loop...

			var group = GetGroup(sizeGroupType);
			var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
			var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
			for(int i = 0; i < displayTexts.Length; i++)
			{
				string display = displayTexts[i];
				// the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
				// so if we're querying a custom size text we substring to only get the name
				// You could see the outputs by just logging
				// Debug.Log(display);
				int pren = display.IndexOf('(');
				if (pren != -1)
					display = display.Substring(0, pren-1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
				if (display == text)
					return i;
			}
			return -1;
		}

		public static bool SizeExists(GameViewSizeGroupType sizeGroupType, int width, int height)
		{
			return FindSize(sizeGroupType, width, height) != -1;
		}

		public static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
		{
			// goal:
			// GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
			// int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
			// iterate through the sizes via group.GetGameViewSize(int index)

			var group = GetGroup(sizeGroupType);
			var groupType = group.GetType();
			var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
			var getCustomCount = groupType.GetMethod("GetCustomCount");
			int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
			var getGameViewSize = groupType.GetMethod("GetGameViewSize");
			var gvsType = getGameViewSize.ReturnType;
			var widthProp = gvsType.GetProperty("width");
			var heightProp = gvsType.GetProperty("height");
			var indexValue = new object[1];
			for(int i = 0; i < sizesCount; i++)
			{
				indexValue[0] = i;
				var size = getGameViewSize.Invoke(group, indexValue);
				int sizeWidth = (int)widthProp.GetValue(size, null);
				int sizeHeight = (int)heightProp.GetValue(size, null);
				if (sizeWidth == width && sizeHeight == height)
					return i;
			}
			return -1;
		}

		static object GetGroup(GameViewSizeGroupType type)
		{
			return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
		}

		public static GameViewSizeGroupType GetCurrentGroupType()
		{
			var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
			return (GameViewSizeGroupType)(int)getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
		}

	}
}
