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

        protected static EnvShapeType ConvertShape(ShapeType shape)
        {
            switch (shape)
            {
                default:
                case ShapeType.Box:
                    return EnvShapeType.Box;
                case ShapeType.Sphere:
                    return EnvShapeType.Sphere;
            }
        }

    
        public abstract ReflectionProbeMode mode { get; }
        public abstract Texture texture { get; }
        // Position of the center of the probe in capture space
        public abstract float dimmer { get; }
        public abstract Vector3 influenceRight { get; }
        public abstract Vector3 influenceUp { get; }
        public abstract Vector3 influenceForward { get; }
        public abstract Vector3 capturePosition { get; }
        public abstract Vector3 influencePosition { get; }
        public abstract EnvShapeType influenceShapeType { get; }
        public abstract Vector3 influenceExtents { get; }
        public abstract Vector3 blendNormalDistancePositive { get; }
        public abstract Vector3 blendNormalDistanceNegative { get; }
        public abstract Vector3 blendDistancePositive { get; }
        public abstract Vector3 blendDistanceNegative { get; }
        public abstract Vector3 boxSideFadePositive { get; }
        public abstract Vector3 boxSideFadeNegative { get; }

        public abstract EnvShapeType proxyShapeType { get; }
        public abstract Vector3 proxyExtents { get; }
        public abstract bool infiniteProjection { get; }
        public abstract Vector3 proxyRight { get; }
        public abstract Vector3 proxyUp { get; }
        public abstract Vector3 proxyForward { get; }
        public abstract Vector3 proxyPosition { get; }
    }

    class VisibleReflectionProbeWrapper : ProbeWrapper
    {
        static HDAdditionalReflectionData defaultHDAdditionalReflectionData { get { return ComponentSingleton<HDAdditionalReflectionData>.instance; } }

        VisibleReflectionProbe probe;
        HDAdditionalReflectionData additional;

        public VisibleReflectionProbeWrapper(VisibleReflectionProbe probe)
        {
            this.probe = probe;
            additional = GetHDAdditionalReflectionData(probe);
        }

        static HDAdditionalReflectionData GetHDAdditionalReflectionData(VisibleReflectionProbe probe)
        {
            var add = probe.probe.GetComponent<HDAdditionalReflectionData>();
            if (add == null)
            {
                add = defaultHDAdditionalReflectionData;
                add.blendDistancePositive = Vector3.one * probe.blendDistance;
                add.blendDistanceNegative = add.blendDistancePositive;
                add.influenceShape = ShapeType.Box;
            }
            return add;
        }

        public override Vector3 influenceRight { get { return probe.localToWorld.GetColumn(0).normalized; } }
        public override Vector3 influenceUp { get { return probe.localToWorld.GetColumn(1).normalized; } }
        public override Vector3 influenceForward { get { return probe.localToWorld.GetColumn(2).normalized; } }
        public override Vector3 capturePosition { get { return probe.localToWorld.GetColumn(3); } }
        public override Vector3 influencePosition { get { return capturePosition + probe.center; } }
        public override Texture texture { get { return probe.texture; } }
        public override ReflectionProbeMode mode { get { return probe.probe.mode; } }
        public override EnvShapeType influenceShapeType { get { return ConvertShape(additional.influenceShape); } }
        public override float dimmer { get { return additional.dimmer; } }
        public override Vector3 influenceExtents
        {
            get
            {
                switch (additional.influenceShape)
                {
                    default:
                    case ShapeType.Box:
                        return probe.bounds.extents;
                    case ShapeType.Sphere:
                        return Vector3.one * additional.influenceSphereRadius;
                }
            }
        }
        public override Vector3 blendNormalDistancePositive { get { return additional.blendNormalDistancePositive; } }
        public override Vector3 blendNormalDistanceNegative { get { return additional.blendNormalDistanceNegative; } }
        public override Vector3 blendDistancePositive { get { return additional.blendDistancePositive; } }
        public override Vector3 blendDistanceNegative { get { return additional.blendDistanceNegative; } }
        public override Vector3 boxSideFadePositive { get { return additional.boxSideFadePositive; } }
        public override Vector3 boxSideFadeNegative { get { return additional.boxSideFadeNegative; } }

        public override EnvShapeType proxyShapeType { get { return influenceShapeType; } }
        public override Vector3 proxyExtents { get { return influenceExtents; } }
        public override bool infiniteProjection { get { return probe.boxProjection == 0; } }
        public override Vector3 proxyRight { get { return influenceRight; } }
        public override Vector3 proxyUp{ get { return influenceUp; } }
        public override Vector3 proxyForward{ get { return influenceForward; } }
        public override Vector3 proxyPosition { get { return influencePosition; } }

    }

    class PlanarReflectionProbeWrapper : ProbeWrapper
    {
        PlanarReflectionProbe probe;

        public PlanarReflectionProbeWrapper(PlanarReflectionProbe probe)
        {
            this.probe = probe;
        }

        public override Vector3 influenceRight { get { return probe.influenceRight; } }
        public override Vector3 influenceUp { get { return probe.influenceUp; } }
        public override Vector3 influenceForward { get { return probe.influenceForward; } }
        public override Vector3 capturePosition { get { return probe.capturePosition; } }
        public override Texture texture { get { return probe.texture; } }
        public override EnvShapeType influenceShapeType { get { return ConvertShape(probe.influenceVolume.shapeType); } }
        public override float dimmer { get { return probe.dimmer; } }
        public override Vector3 influenceExtents
        {
            get
            {
                switch (probe.influenceVolume.shapeType)
                {
                    default:
                    case ShapeType.Box:
                        return probe.influenceVolume.boxBaseSize * 0.5f;
                    case ShapeType.Sphere:
                        return probe.influenceVolume.sphereBaseRadius * Vector3.one;
                }
            }
        }

        public override Vector3 influencePosition { get { return probe.influencePosition; } }
        public override Vector3 blendNormalDistancePositive { get { return probe.influenceVolume.boxInfluenceNormalPositiveFade; } }
        public override Vector3 blendNormalDistanceNegative { get { return probe.influenceVolume.boxInfluenceNormalNegativeFade; } }
        public override Vector3 blendDistancePositive { get { return probe.influenceVolume.boxInfluencePositiveFade; } }
        public override Vector3 blendDistanceNegative { get { return probe.influenceVolume.boxInfluenceNegativeFade; } }
        public override Vector3 boxSideFadePositive { get { return probe.influenceVolume.boxPositiveFaceFade; } }
        public override Vector3 boxSideFadeNegative { get { return probe.influenceVolume.boxNegativeFaceFade; } }
        public override EnvShapeType proxyShapeType { get { return ConvertShape(probe.proxyShape); } }
        public override Vector3 proxyExtents { get { return probe.proxyExtents; } }
        public override bool infiniteProjection { get { return probe.infiniteProjection; } }
        public override ReflectionProbeMode mode { get { return probe.mode; } }
        public override Vector3 proxyRight { get { return probe.proxyRight; } }
        public override Vector3 proxyUp { get { return probe.proxyUp; } }
        public override Vector3 proxyForward { get { return probe.proxyForward; } }
        public override Vector3 proxyPosition { get { return probe.proxyPosition; } }

    }
}
