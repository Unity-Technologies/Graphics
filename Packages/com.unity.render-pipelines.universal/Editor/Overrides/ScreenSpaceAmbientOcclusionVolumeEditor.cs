#if MODERN_SSAO
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusionVolumeOverride))]
    sealed class ScreenSpaceAmbientOcclusionVolumeEditor : UniversalRenderPipelineVolumeComponentEditor
    {
        static class Styles
        {
            public static readonly GUIContent qualitySettings = EditorGUIUtility.TrTextContent("Quality Settings");
            public static readonly GUIContent temporalAccumulation = EditorGUIUtility.TrTextContent("Temporal Accumulation");
            public static readonly GUIContent enable = EditorGUIUtility.TrTextContent("Enable", "Enable temporal filtering to reduce noise and improve stability over time.");
            public static readonly GUIContent minimumRadiusInPixels = EditorGUIUtility.TrTextContent("Minimum Radius (Pixels)", "Minimum radius in pixels guaranteed by GTAO. Acts as a screen-space floor on the world-space Radius so the effect remains visible at a distance.");
            public static readonly GUIContent ghostingMitigation = EditorGUIUtility.TrTextContent("Ghosting Mitigation", "Controls how aggressively temporal accumulation reduces ghosting. Higher values reject more history but can increase noise.");
            public static readonly GUIContent historyLength = EditorGUIUtility.TrTextContent("History Length", "Controls the length of the temporal history. Higher values produce smoother, more stable results but can increase ghosting.");
            public static readonly GUIContent useComputeShader = EditorGUIUtility.TrTextContent("Use Compute Shader", "When enabled, uses compute shaders for GTAO calculation. Provides temporal filtering support and configurable direction/step counts.");
            public static readonly GUIContent spatialFilter = EditorGUIUtility.TrTextContent("Spatial Filter", "Spatial filter used by the GTAO compute path.");
        }

        SerializedDataParameter m_Mode;
        SerializedDataParameter m_Quality;

        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_DirectLightingStrength;
        SerializedDataParameter m_FalloffDistance;
        SerializedDataParameter m_Downsample;
        SerializedDataParameter m_AfterOpaque;
        SerializedDataParameter m_BlurQuality;

        SerializedDataParameter m_SampleCount;

        SerializedDataParameter m_Method;
        SerializedDataParameter m_DepthSource;
        SerializedDataParameter m_NormalQuality;

        SerializedDataParameter m_MinimumRadiusInPixels;
        SerializedDataParameter m_UseComputeShader;
        SerializedDataParameter m_SpatialFilter;

        SerializedDataParameter m_TemporalFilter;
        SerializedDataParameter m_GhostingMitigation;
        SerializedDataParameter m_HistoryLength;

        SerializedDataParameter m_DirectionCount;
        SerializedDataParameter m_StepCount;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ScreenSpaceAmbientOcclusionVolumeOverride>(serializedObject);

            m_Mode = Unpack(o.Find("m_Mode"));
            m_Quality = Unpack(o.Find("m_Quality"));

            m_Intensity = Unpack(o.Find("m_Intensity"));
            m_Radius = Unpack(o.Find("m_Radius"));
            m_DirectLightingStrength = Unpack(o.Find("m_DirectLightingStrength"));
            m_FalloffDistance = Unpack(o.Find("m_FalloffDistance"));
            m_Downsample = Unpack(o.Find("m_Downsample"));
            m_AfterOpaque = Unpack(o.Find("m_AfterOpaque"));
            m_BlurQuality = Unpack(o.Find("m_BlurQuality"));

            m_SampleCount = Unpack(o.Find("m_SampleCount"));

            m_Method = Unpack(o.Find("m_Method"));
            m_DepthSource = Unpack(o.Find("m_DepthSource"));
            m_NormalQuality = Unpack(o.Find("m_NormalQuality"));

            m_MinimumRadiusInPixels = Unpack(o.Find("m_MinimumRadiusInPixels"));
            m_UseComputeShader = Unpack(o.Find("m_UseComputeShader"));
            m_SpatialFilter = Unpack(o.Find("m_SpatialFilter"));

            m_TemporalFilter = Unpack(o.Find("m_TemporalFilter"));
            m_GhostingMitigation = Unpack(o.Find("m_GhostingMitigation"));
            m_HistoryLength = Unpack(o.Find("m_HistoryLength"));

            m_DirectionCount = Unpack(o.Find("m_DirectionCount"));
            m_StepCount = Unpack(o.Find("m_StepCount"));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("SSAO", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            PropertyField(m_Mode);
            bool modeChanged = EditorGUI.EndChangeCheck();
            bool isNone = m_Mode.value.intValue == (int)ScreenSpaceAmbientOcclusionMode.None;
            bool isStandard = m_Mode.value.intValue == (int)ScreenSpaceAmbientOcclusionMode.Standard;
            bool isGTAO = m_Mode.value.intValue == (int)ScreenSpaceAmbientOcclusionMode.GTAO;

            if (isNone)
                return;

            PropertyField(m_Method);
            PropertyField(m_Intensity);
            PropertyField(m_Radius);
            PropertyField(m_FalloffDistance);
            PropertyField(m_DirectLightingStrength);
            PropertyField(m_AfterOpaque);

            if (isGTAO)
            {
                PropertyField(m_MinimumRadiusInPixels, Styles.minimumRadiusInPixels);
                PropertyField(m_UseComputeShader, Styles.useComputeShader);
            }

            bool useComputeShader = isGTAO && m_UseComputeShader.value.boolValue;
            if (useComputeShader)
            {
                using (new IndentLevelScope())
                    PropertyField(m_SpatialFilter, Styles.spatialFilter);
            }

            if (!SystemInfo.supportsComputeShaders && useComputeShader)
            {
                EditorGUILayout.HelpBox("Compute shaders are not supported on this platform. GTAO with compute shaders will not function correctly.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.qualitySettings, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            PropertyField(m_Quality);
            bool qualityChanged = EditorGUI.EndChangeCheck();

            // Apply presets when quality or mode changes, but only if quality override is enabled
            if ((qualityChanged || modeChanged) && m_Quality.overrideState.boolValue)
            {
                var qualityValue = (ScreenSpaceAmbientOcclusionQuality)m_Quality.value.intValue;
                if (qualityValue != ScreenSpaceAmbientOcclusionQuality.Custom)
                {
                    if (isStandard)
                        ApplyStandardPreset(qualityValue);
                    else if (useComputeShader)
                        ApplyGTAOComputePreset(qualityValue);
                    else
                        ApplyGTAOStandardPreset(qualityValue);
                }
            }

            bool isCustom = m_Quality.value.intValue == (int)ScreenSpaceAmbientOcclusionQuality.Custom;

            using (new IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!isCustom))
                {
                    if (isStandard)
                    {
                        PropertyField(m_DepthSource);
                        bool isDepthNormals = m_DepthSource.value.intValue == (int)ScreenSpaceAmbientOcclusionDepthSource.DepthNormals;
                        using (new EditorGUI.DisabledScope(isDepthNormals))
                        {
                            using (new IndentLevelScope())
                                PropertyField(m_NormalQuality);
                        }
                    }

                    PropertyField(m_Downsample);

                    if (isStandard)
                    {
                        PropertyField(m_BlurQuality);
                        PropertyField(m_SampleCount);
                    }

                    if (isGTAO)
                    {
                        if (useComputeShader)
                        {
                            PropertyField(m_DirectionCount);
                            PropertyField(m_StepCount);
                        }
                        else
                        {
                            PropertyField(m_SampleCount);
                        }
                    }
                }
            }

            // Temporal Accumulation is only available in GTAO when using compute shaders
            if (isGTAO && useComputeShader)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.temporalAccumulation, EditorStyles.boldLabel);

                PropertyField(m_TemporalFilter, Styles.enable);
                if (m_TemporalFilter.value.boolValue)
                {
                    using (new IndentLevelScope())
                    {
                        PropertyField(m_GhostingMitigation, Styles.ghostingMitigation);
                        PropertyField(m_HistoryLength, Styles.historyLength);
                    }
                }
            }
        }

        void ApplyStandardPreset(ScreenSpaceAmbientOcclusionQuality quality)
        {
            if (quality == ScreenSpaceAmbientOcclusionQuality.Custom)
                return;

            m_SampleCount.value.intValue = (int)ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetSampleCount(quality);
            m_SampleCount.overrideState.boolValue = true;

            m_DepthSource.value.intValue = (int)ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetDepthSource(quality);
            m_DepthSource.overrideState.boolValue = true;

            m_NormalQuality.value.intValue = (int)ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetNormalQuality(quality);
            m_NormalQuality.overrideState.boolValue = true;

            m_Downsample.value.boolValue = ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetDownsample(quality);
            m_Downsample.overrideState.boolValue = true;

            m_BlurQuality.value.intValue = (int)ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetBlurQuality(quality);
            m_BlurQuality.overrideState.boolValue = true;

            serializedObject.ApplyModifiedProperties();
        }

        void ApplyGTAOStandardPreset(ScreenSpaceAmbientOcclusionQuality quality)
        {
            if (quality == ScreenSpaceAmbientOcclusionQuality.Custom)
                return;

            m_SampleCount.value.intValue = (int)ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetSampleCount(quality);
            m_SampleCount.overrideState.boolValue = true;

            m_Downsample.value.boolValue = ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetDownsample(quality);
            m_Downsample.overrideState.boolValue = true;

            serializedObject.ApplyModifiedProperties();
        }

        void ApplyGTAOComputePreset(ScreenSpaceAmbientOcclusionQuality quality)
        {
            if (quality == ScreenSpaceAmbientOcclusionQuality.Custom)
                return;

            m_Downsample.value.boolValue = ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetDownsample(quality);
            m_Downsample.overrideState.boolValue = true;

            m_DirectionCount.value.intValue = ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetDirectionCount(quality);
            m_DirectionCount.overrideState.boolValue = true;

            m_StepCount.value.intValue = ScreenSpaceAmbientOcclusionVolumeOverride.GetPresetStepCount(quality);
            m_StepCount.overrideState.boolValue = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
