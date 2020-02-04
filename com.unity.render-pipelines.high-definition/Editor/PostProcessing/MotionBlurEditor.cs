using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_SampleCount;

        SerializedDataParameter m_MaxVelocityInPixels;
        SerializedDataParameter m_MinVelInPixels;

        //  Advanced properties 
        SerializedDataParameter m_CameraRotClamp;
        SerializedDataParameter m_DepthCmpScale;
        SerializedDataParameter m_CameraMotionBlur;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find("m_SampleCount"));
            m_MinVelInPixels = Unpack(o.Find(x => x.minimumVelocity));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maximumVelocity));
            m_CameraRotClamp = Unpack(o.Find(x => x.cameraRotationVelocityClamp));
            m_DepthCmpScale = Unpack(o.Find(x => x.depthComparisonExtent));
            m_CameraMotionBlur = Unpack(o.Find(x => x.cameraMotionBlur));
        }

        public override void OnInspectorGUI()
        {
            bool advanced = isInAdvancedMode;

            PropertyField(m_Intensity);

            base.OnInspectorGUI();

            GUI.enabled = useCustomValue;
            PropertyField(m_SampleCount);
            GUI.enabled = true;

            PropertyField(m_MaxVelocityInPixels);
            PropertyField(m_MinVelInPixels);

            if(advanced)
            {
                PropertyField(m_DepthCmpScale);
                PropertyField(m_CameraRotClamp);
                PropertyField(m_CameraMotionBlur);
            }
        }
    }
}
