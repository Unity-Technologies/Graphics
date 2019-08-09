using UnityEditor.Rendering;
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

        }
    }
}

