using UnityEditor.Experimental.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(ScreenSpaceAmbientOcclusionVolume))]
    sealed class ScreenSpaceAmbientOcclusionVolumeEditor : VolumeComponentEditor
    {
        //SerializedDataParameter m_DepthSource;
        SerializedDataParameter m_NormalQuality;
        SerializedDataParameter m_Downsample;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_SampleCount;

        public override void OnEnable()
        {
            PropertyFetcher<ScreenSpaceAmbientOcclusionVolume> volume = new PropertyFetcher<ScreenSpaceAmbientOcclusionVolume>(serializedObject);

            //m_DepthSource = Unpack(volume.Find(x => x.DepthSource));
            m_NormalQuality = Unpack(volume.Find(x => x.NormalQuality));
            m_Downsample    = Unpack(volume.Find(x => x.Downsample));
            m_Intensity     = Unpack(volume.Find(x => x.Intensity));
            m_Radius        = Unpack(volume.Find(x => x.Radius));
            m_SampleCount   = Unpack(volume.Find(x => x.SampleCount));
        }

        public override void OnInspectorGUI()
        {
            //PropertyField(m_DepthSource, ScreenSpaceAmbientOcclusionFeatureEditor.Styles.DepthSource);
            //if (m_DepthSource == DepthSource.Depth)
            {
                PropertyField(m_NormalQuality, ScreenSpaceAmbientOcclusionFeatureEditor.Styles.NormalQuality);
            }
            PropertyField(m_Downsample,  ScreenSpaceAmbientOcclusionFeatureEditor.Styles.DownSample);
            PropertyField(m_Intensity,   ScreenSpaceAmbientOcclusionFeatureEditor.Styles.Intensity);
            PropertyField(m_Radius,      ScreenSpaceAmbientOcclusionFeatureEditor.Styles.Radius);
            PropertyField(m_SampleCount, ScreenSpaceAmbientOcclusionFeatureEditor.Styles.SampleCount);
        }
    }
}
