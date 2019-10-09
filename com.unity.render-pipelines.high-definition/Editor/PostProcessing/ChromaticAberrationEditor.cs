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
            base.OnInspectorGUI();

            PropertyField(m_SpectralLUT);
            PropertyField(m_Intensity);
            GUI.enabled = useCustomValue;
            PropertyField(m_MaxSamples);
            GUI.enabled = true;
        }
    }
}

