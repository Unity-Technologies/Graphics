using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentWithQualityEditor
    {
        static partial class Styles
        {
            public static GUIContent k_NearSampleCount = new GUIContent("Sample Count", "Sets the number of samples to use for the near field.");
            public static GUIContent k_NearMaxBlur = new GUIContent("Max Radius", "Sets the maximum radius the near blur can reach.");
            public static GUIContent k_FarSampleCount = new GUIContent("Sample Count", "Sets the number of samples to use for the far field.");
            public static GUIContent k_FarMaxBlur = new GUIContent("Max Radius", "Sets the maximum radius the far blur can reach");

            public static GUIContent k_NearFocusStart = new GUIContent("Start", "Sets the distance from the Camera at which the near field blur begins to decrease in intensity.");
            public static GUIContent k_FarFocusStart = new GUIContent("Start", "Sets the distance from the Camera at which the far field starts blurring.");

            public static GUIContent k_NearFocusEnd = new GUIContent("End", "Sets the distance from the Camera at which the near field does not blur anymore.");
            public static GUIContent k_FarFocusEnd = new GUIContent("End", "Sets the distance from the Camera at which the far field blur reaches its maximum blur radius.");
            public static GUIContent k_PhysicallyBased = new GUIContent("Physically Based", "Uses a more accurate but slower physically based method to compute DoF.");

            public static GUIContent k_DepthOfFieldMode = new GUIContent("Focus Mode", "Controls the focus of the camera lens.");

            public static readonly string PbrDofResolutionTitle = "Enable High Resolution";
            public static readonly string InfoBox = "Physically Based DoF currently has a high performance overhead. Enabling TAA is highly recommended when using this option.";
            public static readonly string FocusDistanceInfoBox = "When using the Physical Camera mode, the depth of field will be influenced by the Aperture, the Focal Length and the Sensor size set in the physical properties of the camera.";
        }

        SerializedDataParameter m_FocusMode;

        // Physical mode
        SerializedDataParameter m_FocusDistance;

        SerializedDataParameter m_FocusDistanceMode;

        // Manual mode
        SerializedDataParameter m_NearFocusStart;
        SerializedDataParameter m_NearFocusEnd;
        SerializedDataParameter m_FarFocusStart;
        SerializedDataParameter m_FarFocusEnd;

        // Shared settings
        SerializedDataParameter m_NearSampleCount;
        SerializedDataParameter m_NearMaxBlur;
        SerializedDataParameter m_FarSampleCount;
        SerializedDataParameter m_FarMaxBlur;

        // Advanced settings
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Resolution;
        SerializedDataParameter m_PhysicallyBased;
        SerializedDataParameter m_LimitManualRangeNearBlur;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_FocusMode = Unpack(o.Find(x => x.focusMode));

            m_FocusDistance = Unpack(o.Find(x => x.focusDistance));
            m_FocusDistanceMode = Unpack(o.Find(x => x.focusDistanceMode));

            m_NearFocusStart = Unpack(o.Find(x => x.nearFocusStart));
            m_NearFocusEnd = Unpack(o.Find(x => x.nearFocusEnd));
            m_FarFocusStart = Unpack(o.Find(x => x.farFocusStart));
            m_FarFocusEnd = Unpack(o.Find(x => x.farFocusEnd));

            m_NearSampleCount = Unpack(o.Find("m_NearSampleCount"));
            m_NearMaxBlur = Unpack(o.Find("m_NearMaxBlur"));
            m_FarSampleCount = Unpack(o.Find("m_FarSampleCount"));
            m_FarMaxBlur = Unpack(o.Find("m_FarMaxBlur"));

            m_HighQualityFiltering = Unpack(o.Find("m_HighQualityFiltering"));
            m_Resolution = Unpack(o.Find("m_Resolution"));
            m_PhysicallyBased = Unpack(o.Find("m_PhysicallyBased"));
            m_LimitManualRangeNearBlur = Unpack(o.Find("m_LimitManualRangeNearBlur"));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_FocusMode, Styles.k_DepthOfFieldMode);

            int mode = m_FocusMode.value.intValue;
            if (mode == (int)DepthOfFieldMode.Off)
                return;

            using (new IndentLevelScope())
            {
                // Draw the focus mode controls
                DrawFocusSettings(mode);
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();

            using (new IndentLevelScope())
            {
                // Draw the quality controls
                DrawQualitySettings();
            }
        }

        void DrawFocusSettings(int mode)
        {
            if (mode == (int)DepthOfFieldMode.UsePhysicalCamera)
            {
                EditorGUILayout.HelpBox(Styles.FocusDistanceInfoBox, MessageType.Info);
                PropertyField(m_FocusDistanceMode);

                int distanceMode = m_FocusDistanceMode.value.intValue;
                if (distanceMode == (int)FocusDistanceMode.Volume)
                {
                    PropertyField(m_FocusDistance);
                }
            }
            else if (mode == (int)DepthOfFieldMode.Manual)
            {
                EditorGUI.BeginChangeCheck();
                PropertyField(m_NearFocusStart, Styles.k_NearFocusStart);
                if (EditorGUI.EndChangeCheck())
                {
                    float maxBound = m_NearFocusEnd.overrideState.boolValue ? m_NearFocusEnd.value.floatValue :
                        m_FarFocusStart.overrideState.boolValue ? m_FarFocusStart.value.floatValue :
                        m_FarFocusEnd.overrideState.boolValue ? m_FarFocusEnd.value.floatValue : float.MaxValue;
                    if (m_NearFocusStart.value.floatValue >= maxBound)
                        m_NearFocusStart.value.floatValue = maxBound - 1e-5f;
                }

                EditorGUI.BeginChangeCheck();
                PropertyField(m_NearFocusEnd, Styles.k_NearFocusEnd);
                if (EditorGUI.EndChangeCheck())
                {
                    float minBound = m_NearFocusStart.overrideState.boolValue ? m_NearFocusStart.value.floatValue : float.MinValue;
                    if (m_NearFocusEnd.value.floatValue <= minBound)
                        m_NearFocusEnd.value.floatValue = minBound + 1e-5f;

                    float maxBound = m_FarFocusStart.overrideState.boolValue ? m_FarFocusStart.value.floatValue :
                        m_FarFocusEnd.overrideState.boolValue ? m_FarFocusEnd.value.floatValue : float.MaxValue;
                    if (m_NearFocusEnd.value.floatValue >= maxBound)
                        m_NearFocusEnd.value.floatValue = maxBound - 1e-5f;
                }

                EditorGUI.BeginChangeCheck();
                PropertyField(m_FarFocusStart, Styles.k_FarFocusStart);
                if (EditorGUI.EndChangeCheck())
                {
                    float minBound = m_NearFocusEnd.overrideState.boolValue ? m_NearFocusEnd.value.floatValue :
                        m_NearFocusStart.overrideState.boolValue ? m_NearFocusStart.value.floatValue : float.MinValue;
                    if (m_FarFocusStart.value.floatValue <= minBound)
                        m_FarFocusStart.value.floatValue = minBound + 1e-5f;

                    float maxBound = m_FarFocusEnd.overrideState.boolValue ? m_FarFocusEnd.value.floatValue : float.MaxValue;
                    if (m_FarFocusStart.value.floatValue >= maxBound)
                        m_FarFocusStart.value.floatValue = maxBound - 1e-5f;
                }

                EditorGUI.BeginChangeCheck();
                PropertyField(m_FarFocusEnd, Styles.k_FarFocusEnd);
                if (EditorGUI.EndChangeCheck())
                {
                    float minBound = m_FarFocusStart.overrideState.boolValue ? m_FarFocusStart.value.floatValue :
                        m_NearFocusEnd.overrideState.boolValue ? m_NearFocusEnd.value.floatValue :
                        m_NearFocusStart.overrideState.boolValue ? m_NearFocusStart.value.floatValue : float.MinValue;
                    if (m_FarFocusEnd.value.floatValue <= minBound)
                        m_FarFocusEnd.value.floatValue = minBound + 1e-5f;
                }
            }
        }

        void PropertyPBRDofResolution(SerializedDataParameter property)
        {
            using (var scope = new OverridablePropertyScope(property, Styles.PbrDofResolutionTitle, this))
            {
                if (!scope.displayed)
                    return;

                bool isHighResolution = property.value.intValue <= (int)DepthOfFieldResolution.Half;
                isHighResolution = EditorGUILayout.Toggle(Styles.PbrDofResolutionTitle, isHighResolution);
                property.value.intValue = isHighResolution ? Math.Min((int)DepthOfFieldResolution.Half, property.value.intValue) : (int)DepthOfFieldResolution.Quarter;
            }
        }

        void DrawQualitySettings()
        {
            using (new QualityScope(this))
            {
                PropertyField(m_NearSampleCount, Styles.k_NearSampleCount);
                PropertyField(m_NearMaxBlur, Styles.k_NearMaxBlur);
                PropertyField(m_FarSampleCount, Styles.k_FarSampleCount);
                PropertyField(m_FarMaxBlur, Styles.k_FarMaxBlur);
                PropertyField(m_PhysicallyBased);
                if (m_PhysicallyBased.value.boolValue)
                    PropertyPBRDofResolution(m_Resolution);
                else
                    PropertyField(m_Resolution);
                
                PropertyField(m_HighQualityFiltering);
                if (m_PhysicallyBased.value.boolValue)
                {
                    if (BeginAdditionalPropertiesScope())
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox(Styles.InfoBox, MessageType.Info);
                    }
                    EndAdditionalPropertiesScope();
                }

                if (m_FocusMode.value.intValue == (int)DepthOfFieldMode.Manual && !m_PhysicallyBased.value.boolValue)
                {
                    PropertyField(m_LimitManualRangeNearBlur);
                }
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            settings.Save<int>(m_NearSampleCount);
            settings.Save<float>(m_NearMaxBlur);
            settings.Save<int>(m_FarSampleCount);
            settings.Save<float>(m_FarMaxBlur);
            settings.Save<int>(m_Resolution);
            settings.Save<bool>(m_HighQualityFiltering);
            settings.Save<bool>(m_PhysicallyBased);
            settings.Save<bool>(m_LimitManualRangeNearBlur);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            settings.TryLoad<int>(ref m_NearSampleCount);
            settings.TryLoad<float>(ref m_NearMaxBlur);
            settings.TryLoad<int>(ref m_FarSampleCount);
            settings.TryLoad<float>(ref m_FarMaxBlur);
            settings.TryLoad<int>(ref m_Resolution);
            settings.TryLoad<bool>(ref m_HighQualityFiltering);
            settings.TryLoad<bool>(ref m_PhysicallyBased);
            settings.TryLoad<bool>(ref m_LimitManualRangeNearBlur);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            CopySetting(ref m_NearSampleCount, settings.postProcessQualitySettings.NearBlurSampleCount[level]);
            CopySetting(ref m_NearMaxBlur, settings.postProcessQualitySettings.NearBlurMaxRadius[level]);
            CopySetting(ref m_FarSampleCount, settings.postProcessQualitySettings.FarBlurSampleCount[level]);
            CopySetting(ref m_FarMaxBlur, settings.postProcessQualitySettings.FarBlurMaxRadius[level]);
            CopySetting(ref m_Resolution, (int)settings.postProcessQualitySettings.DoFResolution[level]);
            CopySetting(ref m_HighQualityFiltering, settings.postProcessQualitySettings.DoFHighQualityFiltering[level]);
            CopySetting(ref m_PhysicallyBased, settings.postProcessQualitySettings.DoFPhysicallyBased[level]);
            CopySetting(ref m_LimitManualRangeNearBlur, settings.postProcessQualitySettings.LimitManualRangeNearBlur[level]);
        }
    }
}
