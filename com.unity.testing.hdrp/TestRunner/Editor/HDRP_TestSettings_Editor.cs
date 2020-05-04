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

    void SetGameViewSize()
	{
        if (typedTarget == null) return;
        GameViewUtils.SetGameViewSize(typedTarget.ImageComparisonSettings.TargetWidth, typedTarget.ImageComparisonSettings.TargetHeight);
	}
}
