using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;

[DisallowMultipleComponent]
[CustomEditor(typeof(DebugViewController))]
public class DebugViewController_Editor : Editor
{
    SerializedProperty s_settingType;

    SerializedProperty s_gBuffer;
    SerializedProperty s_fullScreenDebugMode;

    SerializedProperty s_lightlayers;

    public void OnEnable()
    {
        s_settingType = serializedObject.FindProperty("settingType");

        s_gBuffer = serializedObject.FindProperty("gBuffer");
        s_fullScreenDebugMode = serializedObject.FindProperty("fullScreenDebugMode");
        s_lightlayers = serializedObject.FindProperty("lightlayers");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        if ( ( (UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset) UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline) != null ) // avoid displaying the following if the assigned RP is not a HDRP
        {
            int i_settingType = s_settingType.intValue;//= (int) (target as DebugViewController).settingType;

            s_settingType.intValue = GUILayout.Toolbar(s_settingType.intValue, new string[] { "Material", "Lighting", "Rendering" });

            // if (MaterialDebugSettings.debugViewMaterialGBufferStrings == null) new MaterialDebugSettings();
            // if (DebugDisplaySettings.renderingFullScreenDebugStrings == null) new DebugDisplaySettings();

            switch ( (DebugViewController.SettingType) s_settingType.intValue )
            {
                case DebugViewController.SettingType.Material :
                    s_gBuffer.intValue = EditorGUILayout.IntPopup(new GUIContent("GBuffer"), s_gBuffer.intValue, MaterialDebugSettings.debugViewMaterialGBufferStrings, MaterialDebugSettings.debugViewMaterialGBufferValues);
                    break;

                case DebugViewController.SettingType.Lighting:
                    s_lightlayers.boolValue = GUILayout.Toggle(s_lightlayers.boolValue, "Light Layers Visualization");
                    break;

                case DebugViewController.SettingType.Rendering:
                    s_fullScreenDebugMode.intValue = EditorGUILayout.IntPopup(new GUIContent("Fullscreen Debug Mode"), s_fullScreenDebugMode.intValue, DebugDisplaySettings.renderingFullScreenDebugStrings, DebugDisplaySettings.renderingFullScreenDebugValues);
                    break;
            }
        }

        if ( serializedObject.ApplyModifiedProperties() )
        {
            serializedObject.Update();
            (target as DebugViewController).SetDebugView();
        }
    }
}
