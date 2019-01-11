using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;



namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enabled;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_SampleCount;

        SerializedDataParameter m_MaxVelocityInPixels;
        SerializedDataParameter m_MinVelInPixels;


        SerializedDataParameter m_CameraRotClamp;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_Enabled = Unpack(o.Find(x => x.enabled));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_MinVelInPixels = Unpack(o.Find(x => x.minVelInPixels));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maxVelocity));
            m_CameraRotClamp = Unpack(o.Find(x => x.cameraRotationVelocityClamp));
        }

        public override void OnInspectorGUI()
        {
            bool advanced = isInAdvancedMode;

            PropertyField(m_Enabled);
            PropertyField(m_Intensity);
            PropertyField(m_SampleCount);

            PropertyField(m_MaxVelocityInPixels);
            PropertyField(m_MinVelInPixels);

            if(advanced)
            {
                PropertyField(m_CameraRotClamp);
            }
        }
    }
}
