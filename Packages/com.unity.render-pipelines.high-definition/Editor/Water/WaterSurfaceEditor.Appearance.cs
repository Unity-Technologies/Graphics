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
        static internal bool WaterBandHasAgitation(WaterSurfaceEditor serialized, Editor owner, int bandIndex)
        {
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            switch (surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    return (bandIndex == 2 ? serialized.m_RipplesWindSpeed.floatValue : serialized.m_LargeWindSpeed.floatValue) > 0.0f;
                case WaterSurfaceType.River:
                    return (bandIndex == 1 ? serialized.m_RipplesWindSpeed.floatValue : serialized.m_LargeWindSpeed.floatValue) > 0.0f;
                case WaterSurfaceType.Pool:
                    return serialized.m_RipplesWindSpeed.floatValue > 0.0f;
            }
            return false;
        }

        static internal void WaterSurfaceAppearanceSection(WaterSurfaceEditor serialized, Editor owner)
        {
            // Grab the type of the surface
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

            EditorGUILayout.PropertyField(serialized.m_CustomMaterial, k_CustomMaterial);

            var material = serialized.m_CustomMaterial.objectReferenceValue as Material;
            if (material != null && !WaterSurface.IsWaterMaterial(material))
                EditorGUILayout.HelpBox("Water only work with a material using a shader created from the Water Master Node in ShaderGraph.", MessageType.Error);

            EditorGUILayout.LabelField("Smoothness", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                serialized.m_StartSmoothness.floatValue = EditorGUILayout.Slider(k_StartSmoothness, serialized.m_StartSmoothness.floatValue, 0.0f, 0.99f);
                using (new IndentLevelScope())
                    serialized.m_EndSmoothness.floatValue = EditorGUILayout.Slider(k_EndSmoothness, serialized.m_EndSmoothness.floatValue, 0.0f, serialized.m_StartSmoothness.floatValue);

                // Fade range
                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SmoothnessFadeRange, k_SmoothnessFadeStart, serialized.m_SmoothnessFadeStart, k_SmoothnessFadeDistance, serialized.m_SmoothnessFadeDistance);
            }

            // Refraction section
            EditorGUILayout.LabelField("Refraction", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.m_RefractionColor, k_RefractionColor);
                serialized.m_MaxRefractionDistance.floatValue = EditorGUILayout.Slider(k_MaxRefractionDistance, serialized.m_MaxRefractionDistance.floatValue, 0.0f, 3.5f);
                serialized.m_AbsorptionDistance.floatValue = EditorGUILayout.Slider(k_AbsorptionDistance, serialized.m_AbsorptionDistance.floatValue, 0.0f, 100.0f);
            }

            EditorGUILayout.LabelField("Scattering", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.m_ScatteringColor, k_ScatteringColor);
                serialized.m_AmbientScattering.floatValue = EditorGUILayout.Slider(k_AmbientScattering, serialized.m_AmbientScattering.floatValue, 0.0f, 1.0f);
                serialized.m_HeightScattering.floatValue = EditorGUILayout.Slider(k_HeightScattering, serialized.m_HeightScattering.floatValue, 0.0f, 1.0f);
                serialized.m_DisplacementScattering.floatValue = EditorGUILayout.Slider(k_DisplacementScattering, serialized.m_DisplacementScattering.floatValue, 0.0f, 1.0f);

                // Given the low amplitude of the pool waves, it doesn't make any sense to have the tip scattering term available to users
                if (surfaceType != WaterSurfaceType.Pool)
                    serialized.m_DirectLightTipScattering.floatValue = EditorGUILayout.Slider(k_DirectLightTipScattering, serialized.m_DirectLightTipScattering.floatValue, 0.0f, 1.0f);
                serialized.m_DirectLightBodyScattering.floatValue = EditorGUILayout.Slider(k_DirectLightBodyScattering, serialized.m_DirectLightBodyScattering.floatValue, 0.0f, 1.0f);
            }

            // Caustics
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Caustics, k_Caustics);
            if (serialized.m_Caustics.boolValue)
            {
                using (new IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_CausticsResolution);
                    int bandCount = HDRenderPipeline.EvaluateBandCount(surfaceType, serialized.m_Ripples.boolValue);

                    if (bandCount != 1)
                    {
                        switch (surfaceType)
                        {
                            case WaterSurfaceType.OceanSeaLake:
                            {
                                serialized.m_CausticsBand.intValue = EditorGUILayout.IntSlider(k_CausticsBandSwell, serialized.m_CausticsBand.intValue, 0, bandCount - 1);
                            }
                            break;
                            case WaterSurfaceType.River:
                                serialized.m_CausticsBand.intValue = EditorGUILayout.IntSlider(k_CausticsBandAgitation, serialized.m_CausticsBand.intValue, 0, bandCount - 1);
                            break;
                            default:
                                break;
                        }
                    }
                    else
                        serialized.m_CausticsBand.intValue = 0;

                    EditorGUILayout.PropertyField(serialized.m_CausticsVirtualPlaneDistance, k_CausticsVirtualPlaneDistance);
                    serialized.m_CausticsVirtualPlaneDistance.floatValue = Mathf.Max(serialized.m_CausticsVirtualPlaneDistance.floatValue, 0.001f);

                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        EditorGUILayout.PropertyField(serialized.m_CausticsIntensity);
                        serialized.m_CausticsIntensity.floatValue = Mathf.Max(serialized.m_CausticsIntensity.floatValue, 0.0f);

                        EditorGUILayout.PropertyField(serialized.m_CausticsPlaneBlendDistance);
                        serialized.m_CausticsPlaneBlendDistance.floatValue = Mathf.Max(serialized.m_CausticsPlaneBlendDistance.floatValue, 0.0f);
                    }

                    // Display an info box if the wind speed is null for the target band
                    if (!WaterBandHasAgitation(serialized, owner, serialized.m_CausticsBand.intValue))
                    {
                        EditorGUILayout.HelpBox("The selected simulation band has currently a null wind speed and will not generate caustics.", MessageType.Info, wide: true);
                    }
                }
            }

            // Under Water Rendering
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_UnderWater);
            using (new IndentLevelScope())
            {
                if (serialized.m_UnderWater.boolValue)
                {
                    // Bounds data
                    if ((WaterGeometryType)serialized.m_GeometryType.enumValueIndex != WaterGeometryType.Infinite)
                    {
                        EditorGUILayout.PropertyField(serialized.m_VolumeBounds);
                        if (serialized.m_VolumeBounds.objectReferenceValue == null)
                        {
                            CoreEditorUtils.DrawFixMeBox(k_AddColliderMessage, MessageType.Warning, "Fix", () =>
                            {
                                WaterSurface ws = (serialized.target as WaterSurface);
                                var menu = new GenericMenu();
                                BoxCollider previousBC = ws.gameObject.GetComponent<BoxCollider>();
                                if (previousBC != null)
                                    menu.AddItem(k_UseBoxColliderPopup, false, () => ws.volumeBounds = previousBC);
                                menu.AddItem(k_AddBoxColliderPopup, false, () => { BoxCollider bc = ws.gameObject.AddComponent<BoxCollider>(); ws.volumeBounds = bc; });
                                menu.ShowAsContext();
                            });
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serialized.m_VolumeDepth);
                        serialized.m_VolumeDepth.floatValue = Mathf.Max(serialized.m_VolumeDepth.floatValue, 0.0f);
                    }

                    // Priority
                    EditorGUILayout.PropertyField(serialized.m_VolumePriority);
                    serialized.m_VolumePriority.intValue = serialized.m_VolumePriority.intValue > 0 ? serialized.m_VolumePriority.intValue : 0;

                    // Transition size
                    EditorGUILayout.PropertyField(serialized.m_TransitionSize);
                    serialized.m_TransitionSize.floatValue = Mathf.Max(serialized.m_TransitionSize.floatValue, 0.0f);

                    // View distance
                    EditorGUILayout.PropertyField(serialized.m_AbsorbtionDistanceMultiplier);
                    serialized.m_AbsorbtionDistanceMultiplier.floatValue = Mathf.Max(serialized.m_AbsorbtionDistanceMultiplier.floatValue, 0.0f);
                }
            }
        }
    }
}
