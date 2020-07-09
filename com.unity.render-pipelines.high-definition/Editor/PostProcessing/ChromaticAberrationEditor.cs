using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(ChromaticAberration))]
    sealed class ChromaticAberrationEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_SpectralLUT;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_MaxSamples;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<ChromaticAberration>(serializedObject);
            m_SpectralLUT = Unpack(o.Find(x => x.spectralLut));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_MaxSamples = Unpack(o.Find("m_MaxSamples"));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_SpectralLUT);
            PropertyField(m_Intensity);
            
            base.OnInspectorGUI();
            using (new HDEditorUtils.IndentScope())
            {
                GUI.enabled = GUI.enabled && base.overrideState;
                DrawQualitySettings();
            }

            GUI.enabled = true;
        }

        void DrawQualitySettings()
        {
            QualitySettingsBlob oldSettings = SaveCustomQualitySettingsAsObject();
            EditorGUI.BeginChangeCheck();

            PropertyField(m_MaxSamples);

            if (EditorGUI.EndChangeCheck())
            {
                QualitySettingsBlob newSettings = SaveCustomQualitySettingsAsObject();

                if (!ChromaticAberrationQualitySettingsBlob.IsEqual(oldSettings as ChromaticAberrationQualitySettingsBlob, newSettings as ChromaticAberrationQualitySettingsBlob))
                    QualitySettingsWereChanged();
            }
        }

        class ChromaticAberrationQualitySettingsBlob : QualitySettingsBlob
        {
            public int maxSamples;

            public ChromaticAberrationQualitySettingsBlob() : base(1) { }

            public static bool IsEqual(ChromaticAberrationQualitySettingsBlob left, ChromaticAberrationQualitySettingsBlob right)
            {
                return QualitySettingsBlob.IsEqual(left, right)
                    && left.maxSamples == right.maxSamples;
            }
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            ChromaticAberrationQualitySettingsBlob qualitySettings = settings as ChromaticAberrationQualitySettingsBlob;

            m_MaxSamples.value.intValue = qualitySettings.maxSamples;
            m_MaxSamples.overrideState.boolValue = qualitySettings.overrideState[0];
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_MaxSamples.value.intValue = settings.postProcessQualitySettings.ChromaticAberrationMaxSamples[level];

            m_MaxSamples.overrideState.boolValue = true;
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob history = null)
        {
            ChromaticAberrationQualitySettingsBlob qualitySettings = (history != null) ? history as ChromaticAberrationQualitySettingsBlob : new ChromaticAberrationQualitySettingsBlob();

            qualitySettings.maxSamples = m_MaxSamples.value.intValue;
            qualitySettings.overrideState[0] = m_MaxSamples.overrideState.boolValue;

            return qualitySettings;
        }
    }
}

