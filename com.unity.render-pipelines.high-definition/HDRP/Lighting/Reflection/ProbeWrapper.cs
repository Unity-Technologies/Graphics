using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class ProbeWrapper
    {
        public static ProbeWrapper Wrap(VisibleReflectionProbe probe, PlanarReflectionProbe planarProbe)
        {
            if (probe.probe != null)
                return new VisibleReflectionProbeWrapper(probe);
            if (planarProbe != null)
                return new PlanarReflectionProbeWrapper(planarProbe);

            throw new ArgumentException();
        }

        protected static EnvShapeType ConvertShape(Shape shape)
        {
            switch (shape)
            {
                default:
                case Shape.Box:
                    return EnvShapeType.Box;
                case Shape.Sphere:
                    return EnvShapeType.Sphere;
            }
        }

        public ReflectionProbe reflectionProbe { get; protected set; }
        public PlanarReflectionProbe planarReflectionProbe { get; protected set; }

        public abstract ReflectionProbeMode mode { get; }
        public abstract Texture texture { get; }
        // Position of the center of the probe in capture space
        public abstract float weight { get; }
        public abstract float multiplier { get; }
        public abstract Matrix4x4 influenceToWorld { get; }
        public abstract EnvShapeType influenceShapeType { get; }
        public abstract Vector3 influenceExtents { get; }
        public abstract Vector3 blendNormalDistancePositive { get; }
        public abstract Vector3 blendNormalDistanceNegative { get; }
        public abstract Vector3 blendDistancePositive { get; }
        public abstract Vector3 blendDistanceNegative { get; }
        public abstract Vector3 boxSideFadePositive { get; }
        public abstract Vector3 boxSideFadeNegative { get; }

        public abstract EnvShapeType proxyShapeType { get; }
        public abstract Matrix4x4 proxyToWorld { get; }
        public abstract Vector3 proxyExtents { get; }
        public abstract bool infiniteProjection { get; }
    }

    class VisibleReflectionProbeWrapper : ProbeWrapper
    {
        VisibleReflectionProbe probe;
        HDAdditionalReflectionData additional;

        public VisibleReflectionProbeWrapper(VisibleReflectionProbe probe)
        {
            this.probe = probe;
            additional = GetHDAdditionalReflectionData(probe);
            reflectionProbe = probe.probe;
        }

        static HDAdditionalReflectionData GetHDAdditionalReflectionData(VisibleReflectionProbe probe)
        {
            var add = probe.probe.GetComponent<HDAdditionalReflectionData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalReflectionData;
                Vector3 distance = Vector3.one * probe.blendDistance;
                add.influenceVolume.boxBlendDistancePositive = distance;
                add.influenceVolume.boxBlendDistanceNegative = distance;
                add.influenceVolume.shape= Shape.Box;
            }
            return add;
        }

        public override Matrix4x4 influenceToWorld
        {
            get
            {
                return Matrix4x4.TRS(
                    (Vector3)probe.localToWorld.GetColumn(3) + probe.center,
                    probe.localToWorld.rotation,
                    Vector3.one
                    );
            }
        }

        public override Texture texture { get { return probe.texture; } }
        public override ReflectionProbeMode mode { get { return probe.probe.mode; } }
        public override EnvShapeType influenceShapeType { get { return ConvertShape(additional.influenceVolume.shape); } }
        public override float weight { get { return additional.weight; } }
        public override float multiplier { get { return additional.multiplier; } }
        public override Vector3 influenceExtents
        {
            get
            {
                switch (additional.influenceVolume.shape)
                {
                    default:
                    case Shape.Box:
                        return probe.bounds.extents;
                    case Shape.Sphere:
                        return Vector3.one * additional.influenceVolume.sphereRadius;
                }
            }
        }
        public override Vector3 blendNormalDistancePositive { get { return additional.influenceVolume.boxBlendNormalDistancePositive; } }
        public override Vector3 blendNormalDistanceNegative { get { return additional.influenceVolume.boxBlendNormalDistanceNegative; } }
        public override Vector3 blendDistancePositive { get { return additional.influenceVolume.boxBlendDistancePositive; } }
        public override Vector3 blendDistanceNegative { get { return additional.influenceVolume.boxBlendDistanceNegative; } }
        public override Vector3 boxSideFadePositive { get { return additional.influenceVolume.boxSideFadePositive; } }
        public override Vector3 boxSideFadeNegative { get { return additional.influenceVolume.boxSideFadeNegative; } }

        public override EnvShapeType proxyShapeType
        {
            get
            {
                return additional.proxyVolume != null
                    ? ConvertShape(additional.proxyVolume.proxyVolume.shapeType)
                    : influenceShapeType;
            }
        }
        public override Vector3 proxyExtents
        {
            get
            {
                return additional.proxyVolume != null
                    ? additional.proxyVolume.proxyVolume.extents
                    : influenceExtents;
            }
        }

        public override bool infiniteProjection
        {
            get
            {
                return additional.proxyVolume != null
                    ? additional.proxyVolume.proxyVolume.infiniteProjection
                    : probe.boxProjection == 0;
            }
        }

        public override Matrix4x4 proxyToWorld
        {
            get
            {
                return additional.proxyVolume != null
                    ? additional.proxyVolume.transform.localToWorldMatrix
                    : influenceToWorld;
            }
        }
    }

    class PlanarReflectionProbeWrapper : ProbeWrapper
    {
        public PlanarReflectionProbeWrapper(PlanarReflectionProbe probe)
        {
            planarReflectionProbe = probe;
        }

        public override Matrix4x4 influenceToWorld { get { return planarReflectionProbe.influenceToWorld; } }
        public override Texture texture { get { return planarReflectionProbe.texture; } }
        public override EnvShapeType influenceShapeType { get { return ConvertShape(planarReflectionProbe.influenceVolume.shape); } }
        public override float weight { get { return planarReflectionProbe.weight; } }
        public override float multiplier { get { return planarReflectionProbe.multiplier; } }
        public override Vector3 influenceExtents
        {
            get
            {
                switch (planarReflectionProbe.influenceVolume.shape)
                {
                    default:
                    case Shape.Box:
                        return planarReflectionProbe.influenceVolume.boxSize * 0.5f;
                    case Shape.Sphere:
                        return planarReflectionProbe.influenceVolume.sphereRadius * Vector3.one;
                }
            }
        }

        public override Vector3 blendNormalDistancePositive { get { return planarReflectionProbe.influenceVolume.boxBlendNormalDistancePositive; } }
        public override Vector3 blendNormalDistanceNegative { get { return planarReflectionProbe.influenceVolume.boxBlendNormalDistanceNegative; } }
        public override Vector3 blendDistancePositive { get { return planarReflectionProbe.influenceVolume.boxBlendDistancePositive; } }
        public override Vector3 blendDistanceNegative { get { return planarReflectionProbe.influenceVolume.boxBlendDistanceNegative; } }
        public override Vector3 boxSideFadePositive { get { return planarReflectionProbe.influenceVolume.boxSideFadePositive; } }
        public override Vector3 boxSideFadeNegative { get { return planarReflectionProbe.influenceVolume.boxSideFadeNegative; } }
        public override EnvShapeType proxyShapeType { get { return ConvertShape(planarReflectionProbe.proxyShape); } }
        public override Vector3 proxyExtents { get { return planarReflectionProbe.proxyExtents; } }
        public override bool infiniteProjection { get { return planarReflectionProbe.infiniteProjection; } }
        public override ReflectionProbeMode mode { get { return planarReflectionProbe.mode; } }

        public override Matrix4x4 proxyToWorld { get { return planarReflectionProbe.proxyToWorld; } }
    }
}
