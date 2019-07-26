using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

// TODO_FCC: Make a base class with quality settings? Both in editor and volume? 

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentEditor
    {
        // Quality settings
        SerializedDataParameter m_UseQualitySettings;
        SerializedDataParameter m_QualitySetting;

        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_SampleCount;

        SerializedDataParameter m_MaxVelocityInPixels;
        SerializedDataParameter m_MinVelInPixels;

        //  Advanced properties 
        SerializedDataParameter m_CameraRotClamp;
        SerializedDataParameter m_DepthCmpScale;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_UseQualitySettings = Unpack(o.Find(x => x.useQualitySettings));
            m_QualitySetting = Unpack(o.Find(x => x.quality));

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find("m_SampleCount"));
            m_MinVelInPixels = Unpack(o.Find(x => x.minimumVelocity));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maximumVelocity));
            m_CameraRotClamp = Unpack(o.Find(x => x.cameraRotationVelocityClamp));
            m_DepthCmpScale = Unpack(o.Find(x => x.depthComparisonExtent));
        }

        public override void OnInspectorGUI()
        {
            bool advanced = isInAdvancedMode;

            PropertyField(m_UseQualitySettings);

            bool useQualitySettings = m_UseQualitySettings.value.boolValue;

            if (useQualitySettings)
                PropertyField(m_QualitySetting);

            PropertyField(m_Intensity);

            if (!useQualitySettings)
                PropertyField(m_SampleCount);

            PropertyField(m_MaxVelocityInPixels);
            PropertyField(m_MinVelInPixels);

            if(advanced)
            {
                PropertyField(m_DepthCmpScale);
                PropertyField(m_CameraRotClamp);
            }
        }
    }
}
