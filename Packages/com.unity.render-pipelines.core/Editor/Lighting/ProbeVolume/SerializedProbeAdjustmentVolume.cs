using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal class SerializedProbeAdjustmentVolume
    {
        internal SerializedProperty shape;
        internal SerializedProperty size;
        internal SerializedProperty radius;

        internal SerializedProperty mode;
        internal SerializedProperty intensityScale;
        internal SerializedProperty overriddenDilationThreshold;
        internal SerializedProperty virtualOffsetRotation;
        internal SerializedProperty virtualOffsetDistance;
        internal SerializedProperty virtualOffsetThreshold;
        internal SerializedProperty geometryBias;
        internal SerializedProperty rayOriginBias;
        internal SerializedProperty skyDirection;

        internal SerializedProperty directSampleCount;
        internal SerializedProperty indirectSampleCount;
        internal SerializedProperty sampleCountMultiplier;
        internal SerializedProperty maxBounces;

        internal SerializedProperty skyOcclusionSampleCount;
        internal SerializedProperty skyOcclusionMaxBounces;

        internal SerializedProperty renderingLayerMaskOperation;
        internal SerializedProperty renderingLayerMask;

        internal SerializedProbeAdjustmentVolume(SerializedObject obj)
        {
            var o = new PropertyFetcher<ProbeAdjustmentVolume>(obj);

            shape = o.Find(x => x.shape);
            size = o.Find(x => x.size);
            radius = o.Find(x => x.radius);

            mode = o.Find(x => x.mode);
            intensityScale = o.Find(x => x.intensityScale);
            overriddenDilationThreshold = o.Find(x => x.overriddenDilationThreshold);
            virtualOffsetRotation = o.Find(x => x.virtualOffsetRotation);
            virtualOffsetDistance = o.Find(x => x.virtualOffsetDistance);
            virtualOffsetThreshold = o.Find(x => x.virtualOffsetThreshold);
            geometryBias = o.Find(x => x.geometryBias);
            rayOriginBias = o.Find(x => x.rayOriginBias);
            skyDirection = o.Find(x => x.skyDirection);

            directSampleCount = o.Find(x => x.directSampleCount);
            indirectSampleCount = o.Find(x => x.indirectSampleCount);
            sampleCountMultiplier = o.Find(x => x.sampleCountMultiplier);
            maxBounces = o.Find(x => x.maxBounces);

            skyOcclusionSampleCount = o.Find(x => x.skyOcclusionSampleCount);
            skyOcclusionMaxBounces = o.Find(x => x.skyOcclusionMaxBounces);

            renderingLayerMaskOperation = o.Find(x => x.renderingLayerMaskOperation);
            renderingLayerMask = o.Find(x => x.renderingLayerMask);
        }
    }
}
