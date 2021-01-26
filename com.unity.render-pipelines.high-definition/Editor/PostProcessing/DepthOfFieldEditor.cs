using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(DepthOfField))]
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

            public static readonly string InfoBox = "Physically Based DoF currently has a high performance overhead. Enabling TAA is highly recommended when using this option.";
        }

        SerializedDataParameter m_FocusMode;

        // Physical mode
        SerializedDataParameter m_FocusDistance;

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

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DepthOfField>(serializedObject);

            m_FocusMode = Unpack(o.Find(x => x.focusMode));

            m_FocusDistance = Unpack(o.Find(x => x.focusDistance));

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

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_FocusMode);

            int mode = m_FocusMode.value.intValue;
            if (mode == (int)DepthOfFieldMode.Off)
                return;

            using (new HDEditorUtils.IndentScope())
            {
                // Draw the focus mode controls
                DrawFocusSettings(mode);
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();

            using (new HDEditorUtils.IndentScope())
            {
                // Draw the quality controls
                DrawQualitySettings();
            }
        }

        void DrawFocusSettings(int mode)
        {
            if (mode == (int)DepthOfFieldMode.UsePhysicalCamera)
            {
                PropertyField(m_FocusDistance);
            }
            else if (mode == (int)DepthOfFieldMode.Manual)
            {
                EditorGUILayout.LabelField("Near Range", EditorStyles.miniLabel);
                PropertyField(m_NearFocusStart, Styles.k_NearFocusStart);
                PropertyField(m_NearFocusEnd, Styles.k_NearFocusEnd);

                EditorGUILayout.LabelField("Far Range", EditorStyles.miniLabel);
                PropertyField(m_FarFocusStart, Styles.k_FarFocusStart);
                PropertyField(m_FarFocusEnd, Styles.k_FarFocusEnd);
            }
        }

        void DrawQualitySettings()
        {
            using (new QualityScope(this))
            {
                EditorGUILayout.LabelField("Near Blur", EditorStyles.miniLabel);
                PropertyField(m_NearSampleCount, Styles.k_NearSampleCount);
                PropertyField(m_NearMaxBlur, Styles.k_NearMaxBlur);

                EditorGUILayout.LabelField("Far Blur", EditorStyles.miniLabel);
                PropertyField(m_FarSampleCount, Styles.k_FarSampleCount);
                PropertyField(m_FarMaxBlur, Styles.k_FarMaxBlur);

                if (isInAdvancedMode)
                {
                    EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);
                    PropertyField(m_Resolution);
                    PropertyField(m_HighQualityFiltering);
                    PropertyField(m_PhysicallyBased);
                    if (m_PhysicallyBased.value.boolValue == true)
                        EditorGUILayout.HelpBox(Styles.InfoBox, MessageType.Info);
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
        }
    }
}
