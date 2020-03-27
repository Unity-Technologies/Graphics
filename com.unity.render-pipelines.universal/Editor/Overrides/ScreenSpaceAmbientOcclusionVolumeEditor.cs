using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(ScreenSpaceAmbientOcclusionVolume))]
    sealed class ScreenSpaceAmbientOcclusionVolumeEditor : VolumeComponentEditor
    {
        //SerializedDataParameter m_DepthSource;
        SerializedDataParameter m_NormalQuality;
        SerializedDataParameter m_DownScale;
        SerializedDataParameter m_Blur;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_SampleCount;

        public override void OnEnable()
        {
            PropertyFetcher<ScreenSpaceAmbientOcclusionVolume> volume = new PropertyFetcher<ScreenSpaceAmbientOcclusionVolume>(serializedObject);

            //m_DepthSource = Unpack(volume.Find(x => x.depthSource));
            m_NormalQuality = Unpack(volume.Find(x => x.NormalQuality));
            m_DownScale     = Unpack(volume.Find(x => x.DownScale));
            m_Blur          = Unpack(volume.Find(x => x.Blur));
            m_Intensity     = Unpack(volume.Find(x => x.Intensity));
            m_Radius        = Unpack(volume.Find(x => x.Radius));
            m_SampleCount   = Unpack(volume.Find(x => x.SampleCount));
        }

        public override void OnInspectorGUI()
        {
            //PropertyField(m_DepthSource);
            //if (m_DepthSource == DepthSource.Depth)
            {
                PropertyField(m_NormalQuality);
            }
            PropertyField(m_DownScale);
            PropertyField(m_Blur);
            PropertyField(m_Intensity);
            PropertyField(m_Radius);
            PropertyField(m_SampleCount);
        }
    }
}
