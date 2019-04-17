using System;
using UnityEngine;
using UnityEditor;

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

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        liveViewSize = GUILayout.Toggle(liveViewSize, "Auto update game view.");

        if ( GUILayout.Button( "Set Game View Size") || liveViewSize && ( prevWidth != typedTarget.ImageComparisonSettings.TargetWidth || prevHeight != typedTarget.ImageComparisonSettings.TargetHeight ) )
        {
            prevWidth = typedTarget.ImageComparisonSettings.TargetWidth;
            prevHeight = typedTarget.ImageComparisonSettings.TargetHeight;

            GameViewUtils.SetTestGameViewSize( prevWidth, prevHeight );
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
}
