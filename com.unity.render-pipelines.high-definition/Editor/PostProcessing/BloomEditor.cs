using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(Bloom))]
    sealed class BloomEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Scatter;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_DirtTexture;
        SerializedDataParameter m_DirtIntensity;

        // Advanced settings
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Resolution;
        SerializedDataParameter m_Anamorphic;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Bloom>(serializedObject);

            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_DirtTexture = Unpack(o.Find(x => x.dirtTexture));
            m_DirtIntensity = Unpack(o.Find(x => x.dirtIntensity));

            m_HighQualityFiltering = Unpack(o.Find("m_HighQualityFiltering"));
            m_Resolution = Unpack(o.Find("m_Resolution"));
            m_Anamorphic = Unpack(o.Find(x => x.anamorphic));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);
            PropertyField(m_Threshold);
            PropertyField(m_Intensity);
            PropertyField(m_Scatter);
            PropertyField(m_Tint);

            EditorGUILayout.LabelField("Lens Dirt", EditorStyles.miniLabel);
            PropertyField(m_DirtTexture, EditorGUIUtility.TrTextContent("Texture"));
            PropertyField(m_DirtIntensity, EditorGUIUtility.TrTextContent("Intensity"));
            
            base.OnInspectorGUI();
            using (new HDEditorUtils.IndentScope())
            {
                GUI.enabled = GUI.enabled && base.overrideState;
                DrawQualitySettings();
            }

            if (isInAdvancedMode)
            {
                EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);

                PropertyField(m_Anamorphic);
            }
        }

        void DrawQualitySettings()
        {
            QualitySettingsBlob oldSettings = SaveCustomQualitySettingsAsObject();
            EditorGUI.BeginChangeCheck();

            PropertyField(m_Resolution);
            PropertyField(m_HighQualityFiltering);

            if (EditorGUI.EndChangeCheck())
            {
                QualitySettingsBlob newSettings = SaveCustomQualitySettingsAsObject();

                if (!BloomQualitySettingsBlob.IsEqual(oldSettings as BloomQualitySettingsBlob, newSettings as BloomQualitySettingsBlob))
                    QualitySettingsWereChanged();
            }
        }

        class BloomQualitySettingsBlob : QualitySettingsBlob
        {
            public BloomResolution resolution;
            public bool hqFiltering;

            public BloomQualitySettingsBlob() : base(2) { }

            public static bool IsEqual(BloomQualitySettingsBlob left, BloomQualitySettingsBlob right)
            {
                return QualitySettingsBlob.IsEqual(left, right)
                    && left.resolution == right.resolution
                    && left.hqFiltering == right.hqFiltering;
            }
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            BloomQualitySettingsBlob qualitySettings = settings as BloomQualitySettingsBlob;

            m_Resolution.value.intValue = (int)qualitySettings.resolution;
            m_HighQualityFiltering.value.boolValue = qualitySettings.hqFiltering;

            m_Resolution.overrideState.boolValue = qualitySettings.overrideState[0];
            m_HighQualityFiltering.overrideState.boolValue = qualitySettings.overrideState[1];
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_Resolution.value.intValue = (int)settings.postProcessQualitySettings.BloomRes[level];
            m_HighQualityFiltering.value.boolValue = settings.postProcessQualitySettings.BloomHighQualityFiltering[level];

            m_Resolution.overrideState.boolValue = true;
            m_HighQualityFiltering.overrideState.boolValue = true;
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob history = null)
        {
            BloomQualitySettingsBlob qualitySettings = (history != null) ? history as BloomQualitySettingsBlob : new BloomQualitySettingsBlob();

            qualitySettings.resolution = (BloomResolution)m_Resolution.value.intValue;
            qualitySettings.hqFiltering = m_HighQualityFiltering.value.boolValue;

            qualitySettings.overrideState[0] = m_Resolution.overrideState.boolValue;
            qualitySettings.overrideState[1] = m_HighQualityFiltering.overrideState.boolValue;

            return qualitySettings;
        }
    }
}
