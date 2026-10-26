using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal class SerializedProbeAdjustmentVolume
    {
        internal SerializedProperty m_Shape;
        internal SerializedProperty m_Size;
        internal SerializedProperty m_Radius;

        internal SerializedProperty m_Mode;
        internal SerializedProperty m_IntensityScale;
        internal SerializedProperty m_OverriddenDilationThreshold;
        internal SerializedProperty m_VirtualOffsetRotation;
        internal SerializedProperty m_VirtualOffsetDistance;
        internal SerializedProperty m_VirtualOffsetThreshold;
        internal SerializedProperty m_GeometryBias;
        internal SerializedProperty m_RayOriginBias;
        internal SerializedProperty m_SkyDirection;

        internal SerializedProperty m_DirectSampleCount;
        internal SerializedProperty m_IndirectSampleCount;
        internal SerializedProperty m_SampleCountMultiplier;
        internal SerializedProperty m_MaxBounces;

        internal SerializedProperty m_SkyOcclusionSampleCount;
        internal SerializedProperty m_SkyOcclusionMaxBounces;

        internal SerializedProperty m_RenderingLayerMaskOperation;
        internal SerializedProperty m_RenderingLayerMask;

        internal SerializedProbeAdjustmentVolume(SerializedObject obj)
        {
            var o = new PropertyFetcher<ProbeAdjustmentVolume>(obj);

            m_Shape = o.Find(x => x.shape);
            m_Size = o.Find(x => x.size);
            m_Radius = o.Find(x => x.radius);

            m_Mode = o.Find(x => x.mode);
            m_IntensityScale = o.Find(x => x.intensityScale);
            m_OverriddenDilationThreshold = o.Find(x => x.overriddenDilationThreshold);
            m_VirtualOffsetRotation = o.Find(x => x.virtualOffsetRotation);
            m_VirtualOffsetDistance = o.Find(x => x.virtualOffsetDistance);
            m_VirtualOffsetThreshold = o.Find(x => x.virtualOffsetThreshold);
            m_GeometryBias = o.Find(x => x.geometryBias);
            m_RayOriginBias = o.Find(x => x.rayOriginBias);
            m_SkyDirection = o.Find(x => x.skyDirection);

            m_DirectSampleCount = o.Find(x => x.directSampleCount);
            m_IndirectSampleCount = o.Find(x => x.indirectSampleCount);
            m_SampleCountMultiplier = o.Find(x => x.sampleCountMultiplier);
            m_MaxBounces = o.Find(x => x.maxBounces);

            m_SkyOcclusionSampleCount = o.Find(x => x.skyOcclusionSampleCount);
            m_SkyOcclusionMaxBounces = o.Find(x => x.skyOcclusionMaxBounces);

            m_RenderingLayerMaskOperation = o.Find(x => x.renderingLayerMaskOperation);
            m_RenderingLayerMask = o.Find(x => x.renderingLayerMask);
        }
    }
}
