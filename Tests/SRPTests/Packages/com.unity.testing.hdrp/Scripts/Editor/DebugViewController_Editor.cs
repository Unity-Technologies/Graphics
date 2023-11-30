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
    SerializedProperty s_lightingFullScreenDebugMode;

    SerializedProperty s_lightingFullScreenDebugRTASView;
    SerializedProperty s_lightingFullScreenDebugRTASMode;

    SerializedProperty s_lightlayers;

    SerializedProperty s_lightingTileClusterDebugMode;
    SerializedProperty s_lightingTileClusterCategory;
    SerializedProperty s_lightingClusterDebugMode;
    SerializedProperty s_lightingClusterDistance;
    SerializedProperty s_lightingShadowDebugMode;

    SerializedProperty s_lightingMaterialOverrideMode;

    public void OnEnable()
    {
        s_settingType = serializedObject.FindProperty("settingType");

        s_gBuffer = serializedObject.FindProperty("gBuffer");
        s_fullScreenDebugMode = serializedObject.FindProperty("fullScreenDebugMode");
        s_lightingFullScreenDebugMode = serializedObject.FindProperty("lightingFullScreenDebugMode");
        s_lightlayers = serializedObject.FindProperty("lightlayers");

        s_lightingFullScreenDebugRTASView = serializedObject.FindProperty("lightingFullScreenDebugRTASView");
        s_lightingFullScreenDebugRTASMode = serializedObject.FindProperty("lightingFullScreenDebugRTASMode");

        s_lightingTileClusterDebugMode = serializedObject.FindProperty("lightingTileClusterDebugMode");
        s_lightingTileClusterCategory = serializedObject.FindProperty("lightingTileClusterCategory");
        s_lightingClusterDebugMode = serializedObject.FindProperty("lightingClusterDebugMode");
        s_lightingClusterDistance = serializedObject.FindProperty("lightingClusterDistance");

        s_lightingShadowDebugMode = serializedObject.FindProperty("lightingShadowDebugMode");

        s_lightingMaterialOverrideMode = serializedObject.FindProperty("lightingMaterialOverrideMode");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        if (((UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline) != null)     // avoid displaying the following if the assigned RP is not a HDRP
        {
            int i_settingType = s_settingType.intValue;//= (int) (target as DebugViewController).settingType;

            s_settingType.intValue = GUILayout.Toolbar(s_settingType.intValue, new string[] { "Material", "Lighting", "Rendering" });

            // if (MaterialDebugSettings.debugViewMaterialGBufferStrings == null) new MaterialDebugSettings();
            // if (DebugDisplaySettings.renderingFullScreenDebugStrings == null) new DebugDisplaySettings();

            switch ((DebugViewController.SettingType)s_settingType.intValue)
            {
                case DebugViewController.SettingType.Material:
                    s_gBuffer.intValue = EditorGUILayout.IntPopup(new GUIContent("GBuffer"), s_gBuffer.intValue, MaterialDebugSettings.debugViewMaterialGBufferStrings, MaterialDebugSettings.debugViewMaterialGBufferValues);
                    break;

                case DebugViewController.SettingType.Lighting:
                    s_lightlayers.boolValue = GUILayout.Toggle(s_lightlayers.boolValue, "Light Layers Visualization");
                    s_lightingFullScreenDebugMode.intValue = EditorGUILayout.IntPopup(new GUIContent("Fullscreen Debug Mode"), s_lightingFullScreenDebugMode.intValue, DebugDisplaySettings.lightingFullScreenDebugStrings, DebugDisplaySettings.lightingFullScreenDebugValues);
                    if ((FullScreenDebugMode)s_lightingFullScreenDebugMode.intValue == FullScreenDebugMode.RayTracingAccelerationStructure)
                    {
                        s_lightingFullScreenDebugRTASView.intValue = (int) (RTASDebugView) EditorGUILayout.EnumPopup(new GUIContent("Ray Tracing Acceleration Structure Debug View"), (RTASDebugView)s_lightingFullScreenDebugRTASView.intValue);
                        s_lightingFullScreenDebugRTASMode.intValue = (int) (RTASDebugMode) EditorGUILayout.EnumPopup(new GUIContent("Ray Tracing Acceleration Structure Debug Mode"), (RTASDebugMode)s_lightingFullScreenDebugRTASMode.intValue);
                    }

                    s_lightingTileClusterDebugMode.intValue = (int) (TileClusterDebug) EditorGUILayout.EnumPopup(new GUIContent("Tile/Cluster Debug Mode"), (TileClusterDebug) s_lightingTileClusterDebugMode.intValue);
                    // Choosing between Tile and Cluster mode
                    if ((TileClusterDebug)s_lightingTileClusterDebugMode.intValue == TileClusterDebug.Cluster || (TileClusterDebug)s_lightingTileClusterDebugMode.intValue == TileClusterDebug.Tile)
                    {
                        s_lightingTileClusterCategory.intValue = (int) (TileClusterCategoryDebug) EditorGUILayout.EnumPopup(new GUIContent("Tile/Cluster Debug By Category"), (TileClusterCategoryDebug)s_lightingTileClusterCategory.intValue);
                        // If we select cluster
                        if ((TileClusterDebug)s_lightingTileClusterDebugMode.intValue == TileClusterDebug.Cluster)
                        {
                            s_lightingClusterDebugMode.intValue = (int) (ClusterDebugMode) EditorGUILayout.EnumPopup(new GUIContent("Cluster Debug Mode"), (ClusterDebugMode)s_lightingClusterDebugMode.intValue);
                            // If we select Visualize Slice, we can set the distance
                            if ((ClusterDebugMode)s_lightingClusterDebugMode.intValue == ClusterDebugMode.VisualizeSlice)
                            {
                                EditorGUILayout.IntSlider(s_lightingClusterDistance, 0, 100, new GUIContent("Cluster Distance"));
                            }
                        }
                    }
                    s_lightingShadowDebugMode.intValue = (int) (ShadowMapDebugMode) EditorGUILayout.EnumPopup(new GUIContent("Shadow Debug Mode"), (ShadowMapDebugMode)s_lightingShadowDebugMode.intValue);
                    s_lightingMaterialOverrideMode.intValue = (int) (DebugViewController.MaterialOverride) EditorGUILayout.EnumFlagsField(new GUIContent("Material Override Mode"), (DebugViewController.MaterialOverride)s_lightingMaterialOverrideMode.intValue);
                    break;

                case DebugViewController.SettingType.Rendering:
                    s_fullScreenDebugMode.intValue = EditorGUILayout.IntPopup(new GUIContent("Fullscreen Debug Mode"), s_fullScreenDebugMode.intValue, DebugDisplaySettings.renderingFullScreenDebugStrings, DebugDisplaySettings.renderingFullScreenDebugValues);
                    break;
            }
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            serializedObject.Update();
            (target as DebugViewController).SetDebugView();
        }
    }
}
