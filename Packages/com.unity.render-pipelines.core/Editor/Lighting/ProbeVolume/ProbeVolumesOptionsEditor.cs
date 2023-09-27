using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(ProbeVolumesOptions))]
    sealed class ProbeVolumesOptionsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_NormalBias;
        SerializedDataParameter m_ViewBias;
        SerializedDataParameter m_ScaleBiasMinProbeDistance;
        SerializedDataParameter m_SamplingNoise;
        SerializedDataParameter m_LeakReductionMode;
        SerializedDataParameter m_MinValidDotProdValue;
        SerializedDataParameter m_AnimateNoise;
        SerializedDataParameter m_OcclusionOnlyNormalization;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ProbeVolumesOptions>(serializedObject);

            m_NormalBias = Unpack(o.Find(x => x.normalBias));
            m_ViewBias = Unpack(o.Find(x => x.viewBias));
            m_ScaleBiasMinProbeDistance = Unpack(o.Find(x => x.scaleBiasWithMinProbeDistance));
            m_SamplingNoise = Unpack(o.Find(x => x.samplingNoise));
            m_LeakReductionMode = Unpack(o.Find(x => x.leakReductionMode));
            m_MinValidDotProdValue = Unpack(o.Find(x => x.minValidDotProductValue));
            m_AnimateNoise = Unpack(o.Find(x => x.animateSamplingNoise));
            m_OcclusionOnlyNormalization = Unpack(o.Find(x => x.occlusionOnlyReflectionNormalization));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_NormalBias);
            PropertyField(m_ViewBias);
            PropertyField(m_ScaleBiasMinProbeDistance);
            PropertyField(m_SamplingNoise);
            PropertyField(m_AnimateNoise);
            PropertyField(m_LeakReductionMode);
            if (m_LeakReductionMode.value.intValue != 0)
            {
                using (new IndentLevelScope())
                    PropertyField(m_MinValidDotProdValue);
            }

            PropertyField(m_OcclusionOnlyNormalization);

        }
    }
}
