using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [VolumeComponentEditor(typeof(ChromaticAberration))]
    sealed class ChromaticAberrationEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ChromaticAberration>(serializedObject);
            m_Intensity = Unpack(o.Find(x => x.intensity));
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Intensity);
        }
    }
}
