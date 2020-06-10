using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(DepthOfField))]
    sealed class DepthOfFieldEditor : VolumeComponentWithQualityEditor
    {
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

            // Draw the focus mode controls
            HDEditorUtils.BeginIndent();
            DrawFocusSettings(mode);
            HDEditorUtils.EndIndent();

            EditorGUILayout.Space();

            // Draw the quality controls
            base.OnInspectorGUI();
            HDEditorUtils.BeginIndent();
            DrawQualitySettings();
            HDEditorUtils.EndIndent();

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
            EditorGUI.BeginChangeCheck();
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

            if (EditorGUI.EndChangeCheck())
            {
                QualitySettingsWereChanged();
            }
        }

        /// An opaque binary blob storing preset settings (used to remember what were the last custom settings that were used).
        /// For the functionality to save and restore the settings <see cref="VolumeComponentWithQualityEditor"/>
        class QualitySettingsBlob
        {
            public int nearSampleCount;
            public float nearMaxBlur;
            public int farSampleCount;
            public float farMaxBlur;
            public DepthOfFieldResolution resolution;
            public bool hqFiltering;
        }

        public override void LoadSettingsFromObject(object settings)
        {
            QualitySettingsBlob qualitySettings = settings as QualitySettingsBlob;

            m_NearSampleCount.value.intValue = qualitySettings.nearSampleCount;
            m_NearMaxBlur.value.floatValue = qualitySettings.nearMaxBlur;
            m_FarSampleCount.value.intValue = qualitySettings.farSampleCount;
            m_FarMaxBlur.value.floatValue = qualitySettings.farMaxBlur;
            m_Resolution.value.intValue = (int) qualitySettings.resolution;
            m_HighQualityFiltering.value.boolValue = qualitySettings.hqFiltering;
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_NearSampleCount.value.intValue = settings.postProcessQualitySettings.NearBlurSampleCount[level];
            m_NearMaxBlur.value.floatValue = settings.postProcessQualitySettings.NearBlurMaxRadius[level];

            m_FarSampleCount.value.intValue = settings.postProcessQualitySettings.FarBlurSampleCount[level];
            m_FarMaxBlur.value.floatValue = settings.postProcessQualitySettings.FarBlurMaxRadius[level];

            m_Resolution.value.intValue = (int) settings.postProcessQualitySettings.DoFResolution[level];
            m_HighQualityFiltering.value.boolValue = settings.postProcessQualitySettings.DoFHighQualityFiltering[level];
        }

        public override object SaveCustomQualitySettingsAsObject(object history)
        {
            QualitySettingsBlob qualitySettings = (history != null) ? history as QualitySettingsBlob : new QualitySettingsBlob();
            
            qualitySettings.nearSampleCount = m_NearSampleCount.value.intValue;
            qualitySettings.nearMaxBlur = m_NearMaxBlur.value.floatValue;
            qualitySettings.farSampleCount = m_FarSampleCount.value.intValue;
            qualitySettings.farMaxBlur = m_FarMaxBlur.value.floatValue;
            qualitySettings.resolution = (DepthOfFieldResolution) m_Resolution.value.intValue;
            qualitySettings.hqFiltering = m_HighQualityFiltering.value.boolValue;
            return qualitySettings;
        }
    }
}
