using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(ProbeVolumesOptions))]
    sealed class ProbeVolumesOptionsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_NormalBias;
        SerializedDataParameter m_ViewBias;
        SerializedDataParameter m_ScaleBiasMinProbeDistance;
        SerializedDataParameter m_SamplingNoise;
        SerializedDataParameter m_LeakReductionMode;
        SerializedDataParameter m_MinValidDotProdValue;
        SerializedDataParameter m_LeakWeightsInfluence;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ProbeVolumesOptions>(serializedObject);

            m_NormalBias = Unpack(o.Find(x => x.normalBias));
            m_ViewBias = Unpack(o.Find(x => x.viewBias));
            m_ScaleBiasMinProbeDistance = Unpack(o.Find(x => x.scaleBiasWithMinProbeDistance));
            m_SamplingNoise = Unpack(o.Find(x => x.samplingNoise));
            m_LeakReductionMode = Unpack(o.Find(x => x.leakReductionMode));
            m_MinValidDotProdValue = Unpack(o.Find(x => x.minValidDotProductValue));
            m_LeakWeightsInfluence = Unpack(o.Find(x => x.leakWeightsInfluence));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_NormalBias);
            PropertyField(m_ViewBias);
            PropertyField(m_ScaleBiasMinProbeDistance);
            PropertyField(m_SamplingNoise);
            PropertyField(m_LeakReductionMode);
            if (m_LeakReductionMode.value.intValue != 0)
            {
                PropertyField(m_LeakWeightsInfluence);
                PropertyField(m_MinValidDotProdValue);
            }
        }
    }
}
