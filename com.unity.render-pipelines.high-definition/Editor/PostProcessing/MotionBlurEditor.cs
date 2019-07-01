using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;



namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentEditor
    {
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

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_MinVelInPixels = Unpack(o.Find(x => x.minimumVelocity));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maximumVelocity));
            m_CameraRotClamp = Unpack(o.Find(x => x.cameraRotationVelocityClamp));
            m_DepthCmpScale = Unpack(o.Find(x => x.depthComparisonExtent));
        }

        public override void OnInspectorGUI()
        {
            bool advanced = isInAdvancedMode;

            PropertyField(m_Intensity);
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
