using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        #region Simulation Mask
        SerializedProperty m_WaterMask;
        SerializedProperty m_WaterMaskRemap;
        SerializedProperty m_WaterMaskExtent;
        SerializedProperty m_WaterMaskOffset;
        #endregion

        #region Large
        SerializedProperty m_RepetitionSize;
        SerializedProperty m_LargeOrientationValue;
        SerializedProperty m_LargeWindSpeed;
        SerializedProperty m_LargeChaos;
        // Band0
        SerializedProperty m_LargeBand0Multiplier;
        SerializedProperty m_LargeBand0FadeToggle;
        SerializedProperty m_LargeBand0FadeStart;
        SerializedProperty m_LargeBand0FadeDistance;
        // Band1
        SerializedProperty m_LargeBand1Multiplier;
        SerializedProperty m_LargeBand1FadeToggle;
        SerializedProperty m_LargeBand1FadeStart;
        SerializedProperty m_LargeBand1FadeDistance;
        // Current
        SerializedProperty m_LargeCurrentSpeedValue;
        SerializedProperty m_LargeCurrentMap;
        SerializedProperty m_LargeCurrentRegionExtent;
        SerializedProperty m_LargeCurrentRegionOffset;
        SerializedProperty m_LargeCurrentMapInfluence;
        #endregion

        #region Ripples
        SerializedProperty m_Ripples;
        SerializedProperty m_RipplesWindSpeed;
        SerializedProperty m_RipplesChaos;
        // Current
        SerializedProperty m_RipplesMotionMode;
        SerializedProperty m_RipplesOrientationValue;
        SerializedProperty m_RipplesCurrentSpeedValue;
        SerializedProperty m_RipplesCurrentMap;
        SerializedProperty m_RipplesCurrentRegionExtent;
        SerializedProperty m_RipplesCurrentRegionOffset;
        SerializedProperty m_RipplesCurrentMapInfluence;
        // Fade
        SerializedProperty m_RipplesFadeToggle;
        SerializedProperty m_RipplesFadeStart;
        SerializedProperty m_RipplesFadeDistance;
        #endregion

        void OnEnableSimulation(PropertyFetcher<WaterSurface> o)
        {
            #region Simulation Mask
            m_WaterMask = o.Find(x => x.waterMask);
            m_WaterMaskRemap = o.Find(x => x.waterMaskRemap);
            m_WaterMaskExtent = o.Find(x => x.waterMaskExtent);
            m_WaterMaskOffset = o.Find(x => x.waterMaskOffset);
            #endregion

            #region Large
            m_RepetitionSize = o.Find(x => x.repetitionSize);
            m_LargeOrientationValue = o.Find(x => x.largeOrientationValue);
            m_LargeWindSpeed = o.Find(x => x.largeWindSpeed);
            m_LargeChaos = o.Find(x => x.largeChaos);

            // Band0
            m_LargeBand0Multiplier = o.Find(x => x.largeBand0Multiplier);
            m_LargeBand0FadeToggle = o.Find(x => x.largeBand0FadeMode);
            m_LargeBand0FadeStart = o.Find(x => x.largeBand0FadeStart);
            m_LargeBand0FadeDistance = o.Find(x => x.largeBand0FadeDistance);

            // Band1
            m_LargeBand1Multiplier = o.Find(x => x.largeBand1Multiplier);
            m_LargeBand1FadeToggle = o.Find(x => x.largeBand1FadeMode);
            m_LargeBand1FadeStart = o.Find(x => x.largeBand1FadeStart);
            m_LargeBand1FadeDistance = o.Find(x => x.largeBand1FadeDistance);

            // Current
            m_LargeCurrentSpeedValue = o.Find(x => x.largeCurrentSpeedValue);
            m_LargeCurrentMap = o.Find(x => x.largeCurrentMap);
            m_LargeCurrentRegionExtent = o.Find(x => x.largeCurrentRegionExtent);
            m_LargeCurrentRegionOffset = o.Find(x => x.largeCurrentRegionOffset);
            m_LargeCurrentMapInfluence = o.Find(x => x.largeCurrentMapInfluence);
            #endregion

            #region Ripples
            m_Ripples = o.Find(x => x.ripples);
            m_RipplesWindSpeed = o.Find(x => x.ripplesWindSpeed);
            m_RipplesChaos = o.Find(x => x.ripplesChaos);

            // Current
            m_RipplesMotionMode = o.Find(x => x.ripplesMotionMode);
            m_RipplesOrientationValue = o.Find(x => x.ripplesOrientationValue);
            m_RipplesCurrentSpeedValue = o.Find(x => x.ripplesCurrentSpeedValue);
            m_RipplesCurrentMap = o.Find(x => x.ripplesCurrentMap);
            m_RipplesCurrentRegionExtent = o.Find(x => x.ripplesCurrentRegionExtent);
            m_RipplesCurrentRegionOffset = o.Find(x => x.ripplesCurrentRegionOffset);
            m_RipplesCurrentMapInfluence = o.Find(x => x.ripplesCurrentMapInfluence);

            // Fade
            m_RipplesFadeToggle = o.Find(x => x.ripplesFadeMode);
            m_RipplesFadeStart = o.Find(x => x.ripplesFadeStart);
            m_RipplesFadeDistance = o.Find(x => x.ripplesFadeDistance);
            #endregion
        }

        static internal void WaterSurfaceLargeCurrent(WaterSurfaceEditor serialized)
        {
            EditorGUILayout.PropertyField(serialized.m_LargeCurrentSpeedValue, k_LargeCurrentSpeed);

            if (GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
                return;

            using (new BoldLabelScope())
                MapWithExtent(serialized.m_LargeCurrentMap, k_LargeCurrentMap, serialized.m_LargeCurrentRegionExtent);

            using (new EditorGUI.IndentLevelScope())
            {
                if (serialized.m_LargeCurrentMap.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(serialized.m_LargeCurrentRegionExtent, k_CurrentRegionExtent);
                    SanitizeExtentsVector2(serialized.m_LargeCurrentRegionExtent);
                    EditorGUILayout.PropertyField(serialized.m_LargeCurrentRegionOffset, k_CurrentRegionOffset);
                    EditorGUILayout.PropertyField(serialized.m_LargeCurrentMapInfluence, k_LargeCurrentMapInfluence);
                }
            }
        }

        static bool HasCustomRipplesCurrent(WaterSurfaceEditor serialized)
        {
            return (WaterPropertyOverrideMode)serialized.m_RipplesMotionMode.enumValueIndex == WaterPropertyOverrideMode.Custom;
        }

        static internal void WaterSurfaceRipplesOrientationCurrentInherit(WaterSurfaceEditor serialized, Editor owner, string[] modeNames)
        {
            using (new BoldLabelScope())
                serialized.m_RipplesMotionMode.enumValueIndex = EditorGUILayout.Popup(k_RipplesMotionInherit, serialized.m_RipplesMotionMode.enumValueIndex, modeNames);

            if (HasCustomRipplesCurrent(serialized))
            {
                using (new EditorGUI.IndentLevelScope())
                    WaterSurfaceRipplesOrientationCurrent(serialized);
            }
        }

        static internal void WaterSurfaceRipplesOrientationCurrent(WaterSurfaceEditor serialized)
        {
            EditorGUILayout.PropertyField(serialized.m_RipplesOrientationValue, k_RipplesOrientation);
            EditorGUILayout.PropertyField(serialized.m_RipplesCurrentSpeedValue, k_RipplesCurrentSpeed);

            if (GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
                return;

            using (new BoldLabelScope())
                MapWithExtent(serialized.m_RipplesCurrentMap, k_RipplesCurrentMap, serialized.m_RipplesCurrentRegionExtent);

            using (new EditorGUI.IndentLevelScope())
            {
                if (serialized.m_RipplesCurrentMap.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(serialized.m_RipplesCurrentRegionExtent, k_CurrentRegionExtent);
                    SanitizeExtentsVector2(serialized.m_RipplesCurrentRegionExtent);
                    EditorGUILayout.PropertyField(serialized.m_RipplesCurrentRegionOffset, k_CurrentRegionOffset);
                    EditorGUILayout.PropertyField(serialized.m_RipplesCurrentMapInfluence, k_RipplesCurrentMapInfluence);
                }
            }
        }

        static internal void WaterSurfaceWaterMask(WaterSurfaceEditor serialized, GUIContent maskContent)
        {
            if (GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
                return;

            using (new BoldLabelScope())
                MapWithExtent(serialized.m_WaterMask, maskContent, serialized.m_WaterMaskExtent);

            using (new EditorGUI.IndentLevelScope())
            {
                if (serialized.m_WaterMask.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(serialized.m_WaterMaskExtent, k_WaterMaskExtent);
                    EditorGUILayout.PropertyField(serialized.m_WaterMaskOffset, k_WaterMaskOffset);

                    Vector2 remap = serialized.m_WaterMaskRemap.vector2Value;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(k_WaterMaskRemap, ref remap.x, ref remap.y, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                        serialized.m_WaterMaskRemap.vector2Value = remap;
                }
            }
        }

        static internal void WaterSurfaceSimulationSection_Ocean(WaterSurfaceEditor serialized, Editor owner)
        {
            // Water masking
            WaterSurfaceWaterMask(serialized, k_WaterMaskSwell);

            // Swell section
            EditorGUILayout.LabelField("Swell", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                // Swell parameters
                EditorGUI.BeginChangeCheck();
                float repetitionSize = EditorGUILayout.Slider(k_SwellRepetitionSize, serialized.m_RepetitionSize.floatValue, WaterConsts.k_SwellMinPatchSize, WaterConsts.k_SwellMaxPatchSize);
                if (EditorGUI.EndChangeCheck() || (repetitionSize != Mathf.Clamp(repetitionSize, WaterConsts.k_SwellMinPatchSize, WaterConsts.k_SwellMaxPatchSize)))
                    serialized.m_RepetitionSize.floatValue = repetitionSize;

                EditorGUILayout.PropertyField(serialized.m_LargeWindSpeed, k_SwellWindSpeed);
                EditorGUILayout.PropertyField(serialized.m_LargeChaos, k_SwellChaos);
                EditorGUILayout.PropertyField(serialized.m_LargeOrientationValue, k_SwellOrientation);

                // Current parameters
                WaterSurfaceLargeCurrent(serialized);

                // Band0 foldout
                float totalAmplitude = 0.0f;
                EditorGUILayout.LabelField("First Band", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    // Evaluate the maximal amplitude that this patch size/wind speed allows
                    EditorGUILayout.PropertyField(serialized.m_LargeBand0Multiplier, k_SwellBand0Mutliplier);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        float maxAmplitudeBand0 = serialized.m_LargeBand0Multiplier.floatValue * WaterSystem.EvaluateMaxAmplitude(serialized.m_RepetitionSize.floatValue, serialized.m_LargeWindSpeed.floatValue);
                        EditorGUILayout.TextField(k_SwellMaxAmplitude, maxAmplitudeBand0.ToString("0.00") + " m", EditorStyles.boldLabel);
                        totalAmplitude += maxAmplitudeBand0;
                    }

                    // The fade parameters are only to be displayed when the additional parameters are
                    if (AdvancedProperties.BeginGroup())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_LargeBand0FadeToggle, k_SwellFadeToggle);

                        if (serialized.m_LargeBand0FadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand0FadeStart, k_SwellFadeDistance, serialized.m_LargeBand0FadeDistance);
                                serialized.m_LargeBand0FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand0FadeStart.floatValue, 0.0f);
                                serialized.m_LargeBand0FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand0FadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                    AdvancedProperties.EndGroup();
                }

                // Second band foldout
                EditorGUILayout.LabelField("Second Band", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    // Evaluate the maximal amplitude that this patch size/wind speed allows
                    EditorGUILayout.PropertyField(serialized.m_LargeBand1Multiplier, k_SwellBand1Mutliplier);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        float swellSecondBandRatio = WaterSystem.EvaluateSwellSecondPatchSize(serialized.m_RepetitionSize.floatValue);
                        float maxAmplitudeBand1 = serialized.m_LargeBand1Multiplier.floatValue * WaterSystem.EvaluateMaxAmplitude(swellSecondBandRatio, serialized.m_LargeWindSpeed.floatValue);
                        EditorGUILayout.TextField(k_SwellMaxAmplitude, maxAmplitudeBand1.ToString("0.00") + " m", EditorStyles.boldLabel);
                        totalAmplitude += maxAmplitudeBand1;
                    }

                    // The fade parameters are only to be displayed when the additional parameters are
                    if (AdvancedProperties.BeginGroup())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_LargeBand1FadeToggle, k_SwellFadeToggle);
                        if (serialized.m_LargeBand1FadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand1FadeStart, k_SwellFadeDistance, serialized.m_LargeBand1FadeDistance);
                                serialized.m_LargeBand1FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand1FadeStart.floatValue, 0.0f);
                                serialized.m_LargeBand1FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand1FadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                    AdvancedProperties.EndGroup();
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(k_SwellTotalAmplitude, totalAmplitude.ToString("0.00") + " m", EditorStyles.boldLabel);
                }
            }

            // Ripples Section
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Ripples, k_RipplesEnable);
            if (serialized.m_Ripples.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_RipplesWindSpeed, k_RipplesWindSpeed);
                    EditorGUILayout.PropertyField(serialized.m_RipplesChaos, k_RipplesChaos);

                    // Current & Orientation
                    WaterSurfaceRipplesOrientationCurrentInherit(serialized, owner, WaterPropertyParameterDrawer.swellModeNames);

                    if (AdvancedProperties.BeginGroup())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                    AdvancedProperties.EndGroup();
                }
            }
        }

        static internal void WaterSurfaceSimulationSection_River(WaterSurfaceEditor serialized, Editor owner)
        {
            // Water masking
            WaterSurfaceWaterMask(serialized, k_WaterMaskAgitation);

            // Agitation foldout
            EditorGUILayout.LabelField("Agitation", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                // Swell parameters
                EditorGUI.BeginChangeCheck();
                float repetitionSize = EditorGUILayout.Slider(k_AgitationRepetitionSize, serialized.m_RepetitionSize.floatValue, WaterConsts.k_AgitationMinPatchSize, WaterConsts.k_AgitationMaxPatchSize);
                if (EditorGUI.EndChangeCheck() || (repetitionSize != Mathf.Clamp(repetitionSize, WaterConsts.k_AgitationMinPatchSize, WaterConsts.k_AgitationMaxPatchSize)))
                    serialized.m_RepetitionSize.floatValue = repetitionSize;

                EditorGUILayout.PropertyField(serialized.m_LargeWindSpeed, k_AgitationWindSpeed);
                EditorGUILayout.PropertyField(serialized.m_LargeChaos, k_AgitationChaos);
                EditorGUILayout.PropertyField(serialized.m_LargeOrientationValue, k_AgitationOrientation);

                // Current parameters
                WaterSurfaceLargeCurrent(serialized);

                // Evaluate the maximal amplitude that this patch size/wind speed allows
                EditorGUILayout.PropertyField(serialized.m_LargeBand0Multiplier, k_AgitationBandMutliplier);
                using (new EditorGUI.DisabledScope(true))
                {
                    float maxAmplitude = serialized.m_LargeBand0Multiplier.floatValue * WaterSystem.EvaluateMaxAmplitude(serialized.m_RepetitionSize.floatValue, serialized.m_LargeWindSpeed.floatValue);
                    EditorGUILayout.TextField(k_AgitationTotalAmplitude, maxAmplitude.ToString("0.00") + " m", EditorStyles.boldLabel);
                }

                // The fade parameters are only to be displayed when the additional parameters are
                if (AdvancedProperties.BeginGroup())
                {
                    // Fade of the agitation
                    using (new BoldLabelScope())
                        EditorGUILayout.PropertyField(serialized.m_LargeBand0FadeToggle, k_SwellFadeToggle);
                    if (serialized.m_LargeBand0FadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_SwellRangeToggle, k_SwellFadeStart, serialized.m_LargeBand0FadeStart, k_SwellFadeDistance, serialized.m_LargeBand0FadeDistance);
                            serialized.m_LargeBand0FadeStart.floatValue = Mathf.Max(serialized.m_LargeBand0FadeStart.floatValue, 0.0f);
                            serialized.m_LargeBand0FadeDistance.floatValue = Mathf.Max(serialized.m_LargeBand0FadeDistance.floatValue, 0.0f);
                        }
                    }
                }
                AdvancedProperties.EndGroup();
            }

            // Ripples Section
            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Ripples, k_RipplesEnable);
            if (serialized.m_Ripples.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_RipplesWindSpeed, k_RipplesWindSpeed);
                    EditorGUILayout.PropertyField(serialized.m_RipplesChaos, k_RipplesChaos);

                    // Orientation & Current
                    WaterSurfaceRipplesOrientationCurrentInherit(serialized, owner, WaterPropertyParameterDrawer.agitationModeNames);

                    if (AdvancedProperties.BeginGroup())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                    AdvancedProperties.EndGroup();
                }
            }
        }

        static internal void WaterSurfaceSimulationSection_Pool(WaterSurfaceEditor serialized, Editor owner)
        {
            // Water Mask
            WaterSurfaceWaterMask(serialized, k_WaterMaskRipples);

            EditorGUILayout.LabelField("Ripples", EditorStyles.boldLabel);
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.m_RipplesWindSpeed, k_RipplesWindSpeed);
                    EditorGUILayout.PropertyField(serialized.m_RipplesChaos, k_RipplesChaos);

                    // Current
                    WaterSurfaceRipplesOrientationCurrent(serialized);

                    if (AdvancedProperties.BeginGroup())
                    {
                        // Fade of the ripples
                        using (new BoldLabelScope())
                            EditorGUILayout.PropertyField(serialized.m_RipplesFadeToggle, k_RipplesFadeToggle);
                        if (serialized.m_RipplesFadeToggle.intValue == (int)WaterSurface.FadeMode.Custom)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                WaterPropertyParameterDrawer.DrawMultiPropertiesGUI(k_RipplesFadeRange, k_RipplesFadeStart, serialized.m_RipplesFadeStart, k_RipplesFadeDistance, serialized.m_RipplesFadeDistance);
                                serialized.m_RipplesFadeStart.floatValue = Mathf.Max(serialized.m_RipplesFadeStart.floatValue, 0.0f);
                                serialized.m_RipplesFadeDistance.floatValue = Mathf.Max(serialized.m_RipplesFadeDistance.floatValue, 0.0f);
                            }
                        }
                    }
                    AdvancedProperties.EndGroup();
                }
            }
        }

        static internal void WaterSurfaceSimulationSection(WaterSurfaceEditor serialized, Editor owner)
        {
            if (serialized.m_SurfaceType.hasMultipleDifferentValues)
                return;

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
        }
    }
}
