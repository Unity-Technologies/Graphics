using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    internal abstract class VolumeComponentWithQualityEditor : VolumeComponentEditor
    {
        // Quality settings
        SerializedDataParameter m_UseQualitySettings;
        SerializedDataParameter m_QualitySetting;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumeComponentWithQuality>(serializedObject);

            m_UseQualitySettings = Unpack(o.Find(x => x.useQualitySettings));
            m_QualitySetting = Unpack(o.Find(x => x.quality));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_UseQualitySettings);

            bool useQualitySettings = m_UseQualitySettings.value.boolValue;

            if (useQualitySettings)
                PropertyField(m_QualitySetting);

        }

        protected bool UsesQualitySettings()
        {
            return m_UseQualitySettings.value.boolValue;
        }
    }

}
