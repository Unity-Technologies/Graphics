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
        static internal void WaterSurfaceSimulationSection_Ocean(WaterSurfaceEditor serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.m_WaterMask, k_WaterMaskSwell);
            if (serialized.m_WaterMask.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serialized.m_WaterMaskExtent);
                EditorGUILayout.PropertyField(serialized.m_WaterMaskOffset);
            }

            // Swell section
            EditorGUILayout.LabelField("Swell", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                // Swell parameters
                serialized.m_RepetitionSize.floatValue = EditorGUILayout.Slider(k_SwellRepetitionSize, serialized.m_RepetitionSize.floatValue,
                                                WaterConsts.k_SwellMinPatchSize,
                                                WaterConsts.k_SwellMaxPatchSize);

                // Wind parameters
                serialized.m_LargeWindSpeed.floatValue = EditorGUILayout.Slider(k_SwellWindSpeed, serialized.m_LargeWindSpeed.floatValue, 0.0f, WaterConsts.k_SwellMaximumWindSpeed);
                serialized.m_LargeWindOrientationValue.floatValue = EditorGUILayout.FloatField(k_SwellWindOrientation, serialized.m_LargeWindOrientationValue.floatValue);
                serialized.m_LargeChaos.floatValue = EditorGUILayout.Slider(k_SwellChaos, serialized.m_LargeChaos.floatValue, 0.0f, 1.0f);

                // Current parameters
                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellCurrent, k_SwellCurrentSpeed, serialized.m_LargeCurrentSpeedValue, k_SwellCurrentOrientation, serialized.m_LargeCurrentOrientationValue);

                // Band0 foldout
                float totalAmplitude = 0.0f;
                EditorGUILayout.LabelField("First Band", EditorStyles.boldLabel);
                using (new IndentLevelScope())
                {
                    // Evaluate the maximal amplitude that this patch size/wind speed allows
                    serialized.m_LargeBand0Multiplier.floatValue = EditorGUILayout.Slider(k_SwellBand0Mutliplier, serialized.m_LargeBand0Multiplier.floatValue, 0.0f, 1.0f);
                    using (new DisabledScope(true))
                    {
                        float maxAmplitudeBand0 = serialized.m_LargeBand0Multiplier.floatValue * HDRenderPipeline.EvaluateMaxAmplitude(serialized.m_RepetitionSize.floatValue, serialized.m_LargeWindSpeed.floatValue);
                        EditorGUILayout.TextField(k_SwellMaxAmplitude, maxAmplitudeBand0.ToString("0.00") + " m", EditorStyles.boldLabel);
                        totalAmplitude += maxAmplitudeBand0;
                    }

                    // The fade parameters are only to be displayed when the additional parameters are
                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_LargeBand0FadeToggle, k_SwellFadeToggle);

                        if (serialized.m_LargeBand0FadeToggle.boolValue)
                        {
                            using (new IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand0FadeStart, k_SwellFadeDistance, serialized.m_LargeBand0FadeDistance);
                                serialized.m_LargeBand0FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand0FadeStart.floatValue, 0.0f);
                                serialized.m_LargeBand0FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand0FadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                }

                // Second band foldout
                EditorGUILayout.LabelField("Second Band", EditorStyles.boldLabel);
                using (new IndentLevelScope())
                {
                    // Evaluate the maximal amplitude that this patch size/wind speed allows
                    serialized.m_LargeBand1Multiplier.floatValue = EditorGUILayout.Slider(k_SwellBand1Mutliplier, serialized.m_LargeBand1Multiplier.floatValue, 0.0f, 1.0f);
                    using (new DisabledScope(true))
                    {
                        float swellSecondBandRatio = HDRenderPipeline.EvaluateSwellSecondPatchSize(serialized.m_RepetitionSize.floatValue);
                        float maxAmplitudeBand1 = serialized.m_LargeBand1Multiplier.floatValue * HDRenderPipeline.EvaluateMaxAmplitude(swellSecondBandRatio, serialized.m_LargeWindSpeed.floatValue);
                        EditorGUILayout.TextField(k_SwellMaxAmplitude, maxAmplitudeBand1.ToString("0.00") + " m", EditorStyles.boldLabel);
                        totalAmplitude += maxAmplitudeBand1;
                    }

                    // The fade parameters are only to be displayed when the additional parameters are
                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_LargeBand1FadeToggle, k_SwellFadeToggle);
                        if (serialized.m_LargeBand1FadeToggle.boolValue)
                        {
                            using (new IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand1FadeStart, k_SwellFadeDistance, serialized.m_LargeBand1FadeDistance);
                                serialized.m_LargeBand1FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand1FadeStart.floatValue, 0.0f);
                                serialized.m_LargeBand1FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand1FadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                }

                using (new DisabledScope(true))
                {
                    EditorGUILayout.TextField(k_SwellTotalAmplitude, totalAmplitude.ToString("0.00") + " m", EditorStyles.boldLabel);
                }
            }

            // Ripples Section
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Ripples, k_RipplesEnable);
            if (serialized.m_Ripples.boolValue)
            {
                using (new IndentLevelScope())
                {
                    // Evaluate the max wind speed that this patch size allows and display the wind speed as a slider
                    serialized.m_RipplesWindSpeed.floatValue = EditorGUILayout.Slider(k_RipplesWindSpeed, serialized.m_RipplesWindSpeed.floatValue, 0.0f, WaterConsts.k_RipplesMaxWindSpeed);
                    WaterPropertyParameterDrawer.Draw(k_RipplesWindOrientationSwell, serialized.m_RipplesWindOrientationMode, serialized.m_RipplesWindOrientationValue, WaterPropertyParameterDrawer.swellModeNames);

                    serialized.m_RipplesChaos.floatValue = EditorGUILayout.Slider(k_RipplesChaos, serialized.m_RipplesChaos.floatValue, 0.0f, 1.0f);

                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesCurrentSwell, serialized.m_RipplesCurrentMode, WaterPropertyParameterDrawer.swellModeNames, k_RipplesCurrentEmpty,
                            k_RipplesCurrentSpeed, serialized.m_RipplesCurrentSpeedValue, k_RipplesCurrentOrientation, serialized.m_RipplesCurrentOrientationValue);

                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.boolValue)
                        {
                            using (new IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                }
            }
        }

        static internal void WaterSurfaceSimulationSection_River(WaterSurfaceEditor serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.m_WaterMask, k_WaterMaskAgitation);
            if (serialized.m_WaterMask.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serialized.m_WaterMaskExtent);
                EditorGUILayout.PropertyField(serialized.m_WaterMaskOffset);
            }

            // Agitation foldout
            EditorGUILayout.LabelField("Agitation", EditorStyles.boldLabel);
            using (new IndentLevelScope())
            {
                // Swell parameters
                serialized.m_RepetitionSize.floatValue = EditorGUILayout.Slider(k_AgitationRepetitionSize, serialized.m_RepetitionSize.floatValue,
                                                WaterConsts.k_AgitationMinPatchSize,
                                                WaterConsts.k_AgitationMaxPatchSize);

                // Wind parameters
                serialized.m_LargeWindSpeed.floatValue = EditorGUILayout.Slider(k_AgitationWindSpeed, serialized.m_LargeWindSpeed.floatValue, 0.0f, WaterConsts.k_SwellMaximumWindSpeed);
                serialized.m_LargeWindOrientationValue.floatValue = EditorGUILayout.FloatField(k_AgitationWindOrientation, serialized.m_LargeWindOrientationValue.floatValue);
                serialized.m_LargeChaos.floatValue = EditorGUILayout.Slider(k_AgitationChaos, serialized.m_LargeChaos.floatValue, 0.0f, 1.0f);

                // Current parameters
                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_AgitationCurrent, k_AgitationCurrentSpeed, serialized.m_LargeCurrentSpeedValue, k_AgitationCurrentOrientation, serialized.m_LargeCurrentOrientationValue);

                // Evaluate the maximal amplitude that this patch size/wind speed allows
                serialized.m_LargeBand0Multiplier.floatValue = EditorGUILayout.Slider(k_AgitationBandMutliplier, serialized.m_LargeBand0Multiplier.floatValue, 0.0f, 1.0f);
                using (new DisabledScope(true))
                {
                    float maxAmplitude = serialized.m_LargeBand0Multiplier.floatValue * HDRenderPipeline.EvaluateMaxAmplitude(serialized.m_RepetitionSize.floatValue, serialized.m_LargeWindSpeed.floatValue);
                    EditorGUILayout.TextField(k_AgitationTotalAmplitude, maxAmplitude.ToString("0.00") + " m", EditorStyles.boldLabel);
                }

                // The fade parameters are only to be displayed when the additional parameters are
                if (WaterSurfaceUI.ShowAdditionalProperties())
                {
                    // Fade of the agitation
                    using (new BoldLabelScope())
                        EditorGUILayout.PropertyField(serialized.m_LargeBand0FadeToggle, k_SwellFadeToggle);
                    if (serialized.m_LargeBand0FadeToggle.boolValue)
                    {
                        using (new IndentLevelScope())
                        {
                            WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand0FadeStart, k_SwellFadeDistance, serialized.m_LargeBand0FadeDistance);
                            serialized.m_LargeBand0FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand0FadeStart.floatValue, 0.0f);
                            serialized.m_LargeBand0FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand0FadeDistance.floatValue, 0.0f);
                        }
                    }
                }
            }

            // Ripples Section
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Ripples, k_RipplesEnable);
            if (serialized.m_Ripples.boolValue)
            {
                using (new IndentLevelScope())
                {
                    // Evaluate the max wind speed that this patch size allows and display the wind speed as a slider
                    serialized.m_RipplesWindSpeed.floatValue = EditorGUILayout.Slider(k_RipplesWindSpeed, serialized.m_RipplesWindSpeed.floatValue, 0.0f, WaterConsts.k_RipplesMaxWindSpeed);
                    WaterPropertyParameterDrawer.Draw(k_RipplesWindOrientationAgitation, serialized.m_RipplesWindOrientationMode, serialized.m_RipplesWindOrientationValue, WaterPropertyParameterDrawer.agitationModeNames);

                    serialized.m_RipplesChaos.floatValue = EditorGUILayout.Slider(k_RipplesChaos, serialized.m_RipplesChaos.floatValue, 0.0f, 1.0f);

                    WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesCurrentAgitation, serialized.m_RipplesCurrentMode, WaterPropertyParameterDrawer.agitationModeNames, k_RipplesCurrentEmpty,
                                                k_RipplesCurrentSpeed, serialized.m_RipplesCurrentSpeedValue, k_RipplesCurrentOrientation, serialized.m_RipplesCurrentOrientationValue);
                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.boolValue)
                        {
                            using (new IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                }
            }
        }

        static internal void WaterSurfaceSimulationSection_Pool(WaterSurfaceEditor serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.m_WaterMask, k_WaterMaskRipples);
            if (serialized.m_WaterMask.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(serialized.m_WaterMaskExtent);
                EditorGUILayout.PropertyField(serialized.m_WaterMaskOffset);
            }

            EditorGUILayout.LabelField("Ripples", EditorStyles.boldLabel);
            {
                using (new IndentLevelScope())
                {
                    bool ripples = serialized.m_Ripples.boolValue;
                    // Evaluate the max wind speed that this patch size allows and display the wind speed as a slider
                    serialized.m_RipplesWindSpeed.floatValue = EditorGUILayout.Slider(k_RipplesWindSpeed, serialized.m_RipplesWindSpeed.floatValue, 0.0f, WaterConsts.k_RipplesMaxWindSpeed);
                    serialized.m_RipplesWindOrientationValue.floatValue = EditorGUILayout.FloatField(k_RipplesWindOrientationOnly, serialized.m_RipplesWindOrientationValue.floatValue);
                    serialized.m_RipplesChaos.floatValue = EditorGUILayout.Slider(k_RipplesChaos, serialized.m_RipplesChaos.floatValue, 0.0f, 1.0f);

                    // Current
                    WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesCurrentOnly,
                                                                        k_RipplesCurrentSpeed, serialized.m_RipplesCurrentSpeedValue,
                                                                        k_RipplesCurrentOrientation, serialized.m_RipplesCurrentOrientationValue);

                    if (WaterSurfaceUI.ShowAdditionalProperties())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.boolValue)
                        {
                            using (new IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                }
            }
        }

        static internal void WaterSurfaceSimulationSection(WaterSurfaceEditor serialized, Editor owner)
        {
            serialized.m_TimeMultiplier.floatValue = EditorGUILayout.Slider(k_TimeMultiplier, serialized.m_TimeMultiplier.floatValue, 0.0f, 10.0f);

            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            switch (surfaceType)
            {
                case WaterSurfaceType.OceanSeaLake:
                    WaterSurfaceSimulationSection_Ocean(serialized, owner);
                break;
                case WaterSurfaceType.River:
                    WaterSurfaceSimulationSection_River(serialized, owner);
                break;
                case WaterSurfaceType.Pool:
                    WaterSurfaceSimulationSection_Pool(serialized, owner);
                break;
            };

            // We only support foam for oceans and rivers
            if (surfaceType == WaterSurfaceType.Pool)
            {
                EditorGUILayout.LabelField("Foam", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Foam rendering is currently not supported for Pools.", MessageType.Info, wide: true);
            }
            else
            {
                // Surface foam
                using (new BoldLabelScope())
                    EditorGUILayout.PropertyField(serialized.m_Foam);

                if (serialized.m_Foam.boolValue)
                {
                    using (new IndentLevelScope())
                    {
                        serialized.m_SimulationFoamAmount.floatValue = EditorGUILayout.Slider(k_SimulationFoamAmount, serialized.m_SimulationFoamAmount.floatValue, 0.0f, 1.0f);
                        // serialized.m_SimulationFoamDrag.floatValue = EditorGUILayout.Slider(k_SimulationFoamDrag, serialized.m_SimulationFoamDrag.floatValue, 0.0f, 1.0f);
                        serialized.m_SimulationFoamSmoothness.floatValue = EditorGUILayout.Slider(k_SimulationFoamSmoothness, serialized.m_SimulationFoamSmoothness.floatValue, 0.0f, 1.0f);

                        // Foam texture
                        EditorGUILayout.PropertyField(serialized.m_FoamTextureTiling, k_FoamTextureTiling);
                        EditorGUILayout.PropertyField(serialized.m_FoamTexture, k_FoamTexture);

                        // Foam masking
                        EditorGUILayout.PropertyField(serialized.m_FoamMask, k_FoamMask);
                        if (serialized.m_FoamMask.objectReferenceValue != null)
                        {
                            using (new IndentLevelScope())
                            {
                                EditorGUILayout.PropertyField(serialized.m_FoamMaskExtent);
                                EditorGUILayout.PropertyField(serialized.m_FoamMaskOffset);
                            }
                        }
                        EditorGUILayout.PropertyField(serialized.m_WindFoamCurve, k_WindFoamCurve);
                    }
                }
            }
        }
    }
}
