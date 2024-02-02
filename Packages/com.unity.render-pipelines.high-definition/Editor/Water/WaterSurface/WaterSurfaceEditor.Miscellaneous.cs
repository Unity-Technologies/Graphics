using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;
using static UnityEditor.Rendering.HighDefinition.HDProbeUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        SerializedProperty m_DebugMode;
        SerializedProperty m_WaterMaskDebugMode;
        SerializedProperty m_WaterCurrentDebugMode;
        SerializedProperty m_CurrentDebugMultiplier;
        SerializedProperty m_WaterFoamDebugMode;
        SerializedProperty m_RenderingLayerMask;

        void OnEnableMiscellaneous(PropertyFetcher<WaterSurface> o)
        {
            m_DebugMode = o.Find(x => x.debugMode);
            m_WaterMaskDebugMode = o.Find(x => x.waterMaskDebugMode);
            m_WaterCurrentDebugMode = o.Find(x => x.waterCurrentDebugMode);
            m_CurrentDebugMultiplier = o.Find(x => x.currentDebugMultiplier);
            m_WaterFoamDebugMode = o.Find(x => x.waterFoamDebugMode);
            m_RenderingLayerMask = o.Find(x => x.renderingLayerMask);
        }

        static internal void WaterSurfaceRenderingMode(WaterSurfaceEditor serialized, Editor owner)
        {
            WaterSurfaceType currentSurfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_DebugMode, k_DebugMode);

            // Depending on the rendering mode display the right info message
            WaterDebugMode debugMode = (WaterDebugMode)(serialized.m_DebugMode.enumValueIndex);

            if (debugMode == WaterDebugMode.WaterMask)
            {
                using (new IndentLevelScope())
                    EditorGUILayout.PropertyField(serialized.m_WaterMaskDebugMode, k_WaterMaskDebugMode);

                WaterMaskDebugMode waterMaskMode = (WaterMaskDebugMode)(serialized.m_WaterMaskDebugMode.enumValueIndex);
                switch (waterMaskMode)
                {
                    case WaterMaskDebugMode.RedChannel:
                        {
                            switch (currentSurfaceType)
                            {
                                case WaterSurfaceType.OceanSeaLake:
                                    {
                                        EditorGUILayout.HelpBox("The Red channel of the water mask will attenuate the first band of the swell.", MessageType.Info, wide: true);
                                    }
                                    break;
                                case WaterSurfaceType.River:
                                    {
                                        EditorGUILayout.HelpBox("The Red channel of the water mask will attenuate the agitation.", MessageType.Info, wide: true);
                                    }
                                    break;
                                case WaterSurfaceType.Pool:
                                    {
                                        EditorGUILayout.HelpBox("The Red channel of the water mask will attenuate the ripples.", MessageType.Info, wide: true);
                                    }
                                    break;
                            }
                        }
                        break;
                    case WaterMaskDebugMode.GreenChannel:
                        {
                            switch (currentSurfaceType)
                            {
                                case WaterSurfaceType.OceanSeaLake:
                                    {
                                        EditorGUILayout.HelpBox("The Green channel of the water mask will attenuate the second band of the swell.", MessageType.Info, wide: true);
                                    }
                                    break;
                                case WaterSurfaceType.River:
                                    {
                                        EditorGUILayout.HelpBox("The Red channel of the water mask will attenuate the ripples.", MessageType.Info, wide: true);
                                    }
                                    break;
                                case WaterSurfaceType.Pool:
                                    {
                                        EditorGUILayout.HelpBox("The selected water surface will not be affected by the Green channel of the water mask.", MessageType.Warning, wide: true);
                                    }
                                    break;
                            }
                        }
                        break;
                    case WaterMaskDebugMode.BlueChannel:
                        {
                            switch (currentSurfaceType)
                            {
                                case WaterSurfaceType.OceanSeaLake:
                                    {
                                        EditorGUILayout.HelpBox("The Blue channel of the water mask will attenuate the ripples.", MessageType.Info, wide: true);
                                    }
                                    break;
                                case WaterSurfaceType.River:
                                case WaterSurfaceType.Pool:
                                    {
                                        EditorGUILayout.HelpBox("The selected water surface will not be affected by the Blue channel of the water mask.", MessageType.Warning, wide: true);
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            else if (debugMode == WaterDebugMode.Current)
            {
                using (new IndentLevelScope())
                {
                    if (currentSurfaceType != WaterSurfaceType.Pool)
                        EditorGUILayout.PropertyField(serialized.m_WaterCurrentDebugMode, k_WaterCurrentDebugMode);
                    EditorGUILayout.PropertyField(serialized.m_CurrentDebugMultiplier, k_CurrentDebugMultiplier);
                    serialized.m_CurrentDebugMultiplier.floatValue = Mathf.Max(serialized.m_CurrentDebugMultiplier.floatValue, 0.1f);
                }
            }
            else if (debugMode == WaterDebugMode.Foam)
            {
                EditorGUILayout.PropertyField(serialized.m_WaterFoamDebugMode, k_WaterFoamDebugMode);
            }
        }

        static internal void WaterSurfaceMiscellaneousSection(WaterSurfaceEditor serialized, Editor owner)
        {
            if (HDRenderPipeline.currentPipeline != null)
            {
                bool lightLayersEnabled = HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportLightLayers;
                bool decalLayersEnabled = HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportDecals &&
                    HDRenderPipeline.currentPipeline.currentPlatformRenderPipelineSettings.supportDecalLayers;

                using (new EditorGUI.DisabledScope(!decalLayersEnabled && !lightLayersEnabled))
                    EditorGUILayout.PropertyField(serialized.m_RenderingLayerMask);

                if (!decalLayersEnabled)
                {
                    HDEditorUtils.QualitySettingsHelpBox("Enable 'Decal Layers' in your HDRP Asset if you want to control which decals affect water surfaces. There is a performance cost of enabling this option.",
                        MessageType.Info,
                        HDRenderPipelineUI.ExpandableGroup.Rendering,
                        HDRenderPipelineUI.ExpandableRendering.Decal, "m_RenderPipelineSettings.supportDecalLayers");
                    EditorGUILayout.Space();
                }

                if (!lightLayersEnabled)
                {
                    HDEditorUtils.QualitySettingsHelpBox("Enable 'Light Layers' in your HDRP Asset if you want to control which lights affect water surfaces. There is a performance cost of enabling this option.",
                        MessageType.Info, HDRenderPipelineUI.ExpandableGroup.Lighting, "m_RenderPipelineSettings.supportLightLayers");
                    EditorGUILayout.Space();
                }
            }

            // Display the debugging rendering modes
            WaterSurfaceRenderingMode(serialized, owner);
        }
    }
}
