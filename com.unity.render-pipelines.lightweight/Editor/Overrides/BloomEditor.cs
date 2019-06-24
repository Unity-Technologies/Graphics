using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [VolumeComponentEditor(typeof(Bloom))]
    sealed class BloomEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Scatter;
        SerializedDataParameter m_Clamp;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_DirtTexture;
        SerializedDataParameter m_DirtIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Bloom>(serializedObject);

            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Clamp = Unpack(o.Find(x => x.clamp));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_HighQualityFiltering = Unpack(o.Find(x => x.highQualityFiltering));
            m_DirtTexture = Unpack(o.Find(x => x.dirtTexture));
            m_DirtIntensity = Unpack(o.Find(x => x.dirtIntensity));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);

            PropertyField(m_Threshold);
            PropertyField(m_Intensity);
            PropertyField(m_Scatter);
            PropertyField(m_Tint);
            PropertyField(m_Clamp);
            PropertyField(m_HighQualityFiltering);

            EditorGUILayout.LabelField("Lens Dirt", EditorStyles.miniLabel);

            PropertyField(m_DirtTexture);
            PropertyField(m_DirtIntensity);
        }
    }
}
