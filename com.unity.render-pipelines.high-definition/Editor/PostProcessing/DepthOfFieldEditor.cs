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

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

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
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_FocusMode);

            int mode = m_FocusMode.value.intValue;
            if (mode == (int)DepthOfFieldMode.Off)
            {
                GUI.enabled = false;
            }

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
                GUI.enabled = GUI.enabled && base.overrideState;
                DrawQualitySettings();
            }

            GUI.enabled = true;
        }

        void DrawFocusSettings(int mode)
        {
            if (mode == (int)DepthOfFieldMode.Off)
            {
                // When DoF is off, display a focus distance at infinity
                var val = m_FocusDistance.value.floatValue;
                m_FocusDistance.value.floatValue = Mathf.Infinity;
                PropertyField(m_FocusDistance);
                m_FocusDistance.value.floatValue = val;
            }
            else if (mode == (int)DepthOfFieldMode.UsePhysicalCamera)
            {
                PropertyField(m_FocusDistance);
            }
            else if (mode == (int)DepthOfFieldMode.Manual)
            {
                EditorGUILayout.LabelField("Near Range", EditorStyles.miniLabel);
                PropertyField(m_NearFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_NearFocusEnd, EditorGUIUtility.TrTextContent("End"));

                EditorGUILayout.LabelField("Far Range", EditorStyles.miniLabel);
                PropertyField(m_FarFocusStart, EditorGUIUtility.TrTextContent("Start"));
                PropertyField(m_FarFocusEnd, EditorGUIUtility.TrTextContent("End"));
            }
        }

        void DrawQualitySettings()
        {
            using (new QualityScope(this))
            {
                EditorGUILayout.LabelField("Near Blur", EditorStyles.miniLabel);
                PropertyField(m_NearSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                PropertyField(m_NearMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));

                EditorGUILayout.LabelField("Far Blur", EditorStyles.miniLabel);
                PropertyField(m_FarSampleCount, EditorGUIUtility.TrTextContent("Sample Count"));
                PropertyField(m_FarMaxBlur, EditorGUIUtility.TrTextContent("Max Radius"));

                if (isInAdvancedMode)
                {
                    EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);
                    PropertyField(m_Resolution);
                    PropertyField(m_HighQualityFiltering);
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
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_NearSampleCount.value.intValue = settings.postProcessQualitySettings.NearBlurSampleCount[level];
            m_NearMaxBlur.value.floatValue = settings.postProcessQualitySettings.NearBlurMaxRadius[level];

            m_FarSampleCount.value.intValue = settings.postProcessQualitySettings.FarBlurSampleCount[level];
            m_FarMaxBlur.value.floatValue = settings.postProcessQualitySettings.FarBlurMaxRadius[level];

            m_Resolution.value.intValue = (int)settings.postProcessQualitySettings.DoFResolution[level];
            m_HighQualityFiltering.value.boolValue = settings.postProcessQualitySettings.DoFHighQualityFiltering[level];

            // set all quality override states to true, to indicate that these values are actually used
            m_NearSampleCount.overrideState.boolValue = true;
            m_NearMaxBlur.overrideState.boolValue = true;
            m_FarSampleCount.overrideState.boolValue = true;
            m_FarMaxBlur.overrideState.boolValue = true;
            m_Resolution.overrideState.boolValue = true;
            m_HighQualityFiltering.overrideState.boolValue = true;
        }
    }
}
