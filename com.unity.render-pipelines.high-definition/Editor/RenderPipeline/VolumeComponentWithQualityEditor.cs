using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal abstract class VolumeComponentWithQualityEditor : VolumeComponentEditor
    {
        // Quality settings
        SerializedDataParameter m_QualitySetting;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<VolumeComponentWithQuality>(serializedObject);
            m_QualitySetting = Unpack(o.Find(x => x.quality));
        }

        public override void OnInspectorGUI() =>PropertyField(m_QualitySetting);

        protected bool useCustomValue => m_QualitySetting.value.intValue == ScalableSettingLevelParameter.LevelCount;
    }

}
