using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;


 // All params need renaming...

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;

        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_MaxVelocityInPixels;

        SerializedDataParameter m_MinVelInPixels;
        SerializedDataParameter m_TileMinMaxVelRatioForHighQuality;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_MinVelInPixels = Unpack(o.Find(x => x.minVelInPixels));
            m_MaxVelocityInPixels = Unpack(o.Find(x => x.maxVelocity));
            m_TileMinMaxVelRatioForHighQuality = Unpack(o.Find(x => x.tileMinMaxVelRatioForHighQuality));
        }

        public override void OnInspectorGUI()
        {

            bool advanced = isInAdvancedMode;

            EditorGUILayout.HelpBox("Motion Blur is still heavily WIP and not ready for use in production. To test it regardless, parameters are all under advanced.", MessageType.Warning);

            if (advanced)
            {
                PropertyField(m_Intensity);
                PropertyField(m_SampleCount);
                PropertyField(m_MaxVelocityInPixels);

                PropertyField(m_MinVelInPixels);
                PropertyField(m_TileMinMaxVelRatioForHighQuality);
            }
        }
    }
}
