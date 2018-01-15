using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Experimental.Rendering;
using UnityEditor.Experimental.Rendering.HDPipeline;

[DisallowMultipleComponent]
[CustomEditor(typeof(DebugViewController))]
public class DebugViewController_Editor : Editor
{
    SerializedProperty s_settingType;

    SerializedProperty s_gBuffer;
    SerializedProperty s_fullScreenDebugMode;

    public void OnEnable()
    {
        s_settingType = serializedObject.FindProperty("settingType");

        s_gBuffer = serializedObject.FindProperty("gBuffer");
        s_fullScreenDebugMode = serializedObject.FindProperty("fullScreenDebugMode");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        //EditorGUILayout.PropertyField(s_settingType);
        int i_settingType = s_settingType.intValue;//= (int) (target as DebugViewController).settingType;

        s_settingType.intValue = GUILayout.Toolbar(s_settingType.intValue, new string[] { "Material", "Rendering" });

        switch ( (DebugViewController.SettingType) s_settingType.intValue )
        {
            case DebugViewController.SettingType.Material :
                s_gBuffer.intValue = EditorGUILayout.IntPopup(new GUIContent("GBuffer"), s_gBuffer.intValue, MaterialDebugSettings.debugViewMaterialGBufferStrings, MaterialDebugSettings.debugViewMaterialGBufferValues);
                break;

            case DebugViewController.SettingType.Rendering:
            s_fullScreenDebugMode.intValue = EditorGUILayout.IntPopup(new GUIContent("GBuffer"), s_fullScreenDebugMode.intValue, DebugDisplaySettings.renderingFullScreenDebugStrings, DebugDisplaySettings.renderingFullScreenDebugValues);
                break;
        }

        if ( serializedObject.ApplyModifiedProperties() )
        {
            serializedObject.Update();
            (target as DebugViewController).SetDebugView();
        }
    }
}
