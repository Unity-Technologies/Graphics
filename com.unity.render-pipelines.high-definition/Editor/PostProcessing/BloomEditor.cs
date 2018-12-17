using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(Bloom))]
    sealed class BloomEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Scatter;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_DirtTexture;
        SerializedDataParameter m_DirtIntensity;

        // Advanced settings
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_Resolution;
        SerializedDataParameter m_Prefilter;
        SerializedDataParameter m_Anamorphic;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Bloom>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_DirtTexture = Unpack(o.Find(x => x.dirtTexture));
            m_DirtIntensity = Unpack(o.Find(x => x.dirtIntensity));

            m_HighQualityFiltering = Unpack(o.Find(x => x.highQualityFiltering));
            m_Resolution = Unpack(o.Find(x => x.resolution));
            m_Prefilter = Unpack(o.Find(x => x.prefilter));
            m_Anamorphic = Unpack(o.Find(x => x.anamorphic));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);
            PropertyField(m_Intensity);
            PropertyField(m_Scatter);
            PropertyField(m_Tint);

            EditorGUILayout.LabelField("Lens Dirt", EditorStyles.miniLabel);
            PropertyField(m_DirtTexture, EditorGUIUtility.TrTextContent("Texture"));
            PropertyField(m_DirtIntensity, EditorGUIUtility.TrTextContent("Intensity"));

            if (isInAdvancedMode)
            {
                EditorGUILayout.LabelField("Advanced Tweaks", EditorStyles.miniLabel);
                PropertyField(m_Resolution);
                PropertyField(m_HighQualityFiltering);
                PropertyField(m_Prefilter);
                PropertyField(m_Anamorphic);
            }
        }
    }
}
