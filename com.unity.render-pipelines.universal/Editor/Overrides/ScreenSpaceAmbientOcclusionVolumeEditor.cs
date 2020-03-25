using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(ScreenSpaceAmbientOcclusionVolume))]
    sealed class ScreenSpaceAmbientOcclusionVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_DownScale;
        SerializedDataParameter m_SampleCount;
        //SerializedDataParameter m_DepthSource;
        SerializedDataParameter m_NormalQuality;

        public override void OnEnable()
        {
            PropertyFetcher<ScreenSpaceAmbientOcclusionVolume> volume = new PropertyFetcher<ScreenSpaceAmbientOcclusionVolume>(serializedObject);

            //m_DepthSource = Unpack(volume.Find(x => x.depthSource));
            m_NormalQuality = Unpack(volume.Find(x => x.NormalQuality));
            m_Intensity     = Unpack(volume.Find(x => x.Intensity));
            m_Radius        = Unpack(volume.Find(x => x.Radius));
            m_DownScale     = Unpack(volume.Find(x => x.DownScale));
            m_SampleCount   = Unpack(volume.Find(x => x.SampleCount));
        }

        public override void OnInspectorGUI()
        {
            //PropertyField(m_DepthSource);
            PropertyField(m_DownScale);
            //if (m_DepthSource == DepthSource.Depth)
            {
                PropertyField(m_NormalQuality);
            }
            PropertyField(m_Intensity);
            PropertyField(m_Radius);
            PropertyField(m_SampleCount);
        }
    }
}
