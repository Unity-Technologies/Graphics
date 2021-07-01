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
            var o = new PropertyFetcher<ChromaticAberration>(serializedObject);

            m_SpectralLUT = Unpack(o.Find(x => x.spectralLut));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_MaxSamples = Unpack(o.Find("m_MaxSamples"));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_SpectralLUT);
            PropertyField(m_Intensity);

            base.OnInspectorGUI();

            using (new IndentLevelScope())
            using (new QualityScope(this))
            {
                PropertyField(m_MaxSamples);
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            settings.Save<int>(m_MaxSamples);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            settings.TryLoad<int>(ref m_MaxSamples);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            CopySetting(ref m_MaxSamples, settings.postProcessQualitySettings.ChromaticAberrationMaxSamples[level]);
        }
    }
}
