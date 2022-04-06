using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(Vignette))]
    sealed class VignetteEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Vignette>(serializedObject);
            m_Intensity = Unpack(o.Find(x => x.intensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Intensity);
        }
    }
}
