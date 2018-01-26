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

        public ReflectionProbe reflectionProbe { get; protected set; }
        public PlanarReflectionProbe planarReflectionProbe { get; protected set; }

        public abstract ReflectionProbeMode mode { get; }
        public abstract Texture texture { get; }
        // Position of the center of the probe in capture space
        public abstract float dimmer { get; }
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
        static HDAdditionalReflectionData defaultHDAdditionalReflectionData { get { return ComponentSingleton<HDAdditionalReflectionData>.instance; } }

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
                add = defaultHDAdditionalReflectionData;
                add.blendDistancePositive = Vector3.one * probe.blendDistance;
                add.blendDistanceNegative = add.blendDistancePositive;
                add.influenceShape = ShapeType.Box;
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

        public override EnvShapeType proxyShapeType
        {
            get
            {
                return additional.proxyVolumeComponent != null
                    ? ConvertShape(additional.proxyVolumeComponent.proxyVolume.shapeType)
                    : influenceShapeType;
            }
        }
        public override Vector3 proxyExtents
        {
            get
            {
                return additional.proxyVolumeComponent != null
                    ? additional.proxyVolumeComponent.proxyVolume.boxSize * 0.5f
                    : influenceExtents;
            }
        }

        public override bool infiniteProjection
        {
            get
            {
                return additional.proxyVolumeComponent != null
                    ? additional.proxyVolumeComponent.proxyVolume.infiniteProjection
                    : probe.boxProjection == 0;
            }
        }

        public override Matrix4x4 proxyToWorld
        {
            get
            {
                return additional.proxyVolumeComponent != null
                    ? additional.proxyVolumeComponent.transform.localToWorldMatrix
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
        public override EnvShapeType influenceShapeType { get { return ConvertShape(planarReflectionProbe.influenceVolume.shapeType); } }
        public override float dimmer { get { return planarReflectionProbe.dimmer; } }
        public override Vector3 influenceExtents
        {
            get
            {
                switch (planarReflectionProbe.influenceVolume.shapeType)
                {
                    default:
                    case ShapeType.Box:
                        return planarReflectionProbe.influenceVolume.boxBaseSize * 0.5f;
                    case ShapeType.Sphere:
                        return planarReflectionProbe.influenceVolume.sphereBaseRadius * Vector3.one;
                }
            }
        }

        public override Vector3 blendNormalDistancePositive { get { return planarReflectionProbe.influenceVolume.boxInfluenceNormalPositiveFade; } }
        public override Vector3 blendNormalDistanceNegative { get { return planarReflectionProbe.influenceVolume.boxInfluenceNormalNegativeFade; } }
        public override Vector3 blendDistancePositive { get { return planarReflectionProbe.influenceVolume.boxInfluencePositiveFade; } }
        public override Vector3 blendDistanceNegative { get { return planarReflectionProbe.influenceVolume.boxInfluenceNegativeFade; } }
        public override Vector3 boxSideFadePositive { get { return planarReflectionProbe.influenceVolume.boxPositiveFaceFade; } }
        public override Vector3 boxSideFadeNegative { get { return planarReflectionProbe.influenceVolume.boxNegativeFaceFade; } }
        public override EnvShapeType proxyShapeType { get { return ConvertShape(planarReflectionProbe.proxyShape); } }
        public override Vector3 proxyExtents { get { return planarReflectionProbe.proxyExtents; } }
        public override bool infiniteProjection { get { return planarReflectionProbe.infiniteProjection; } }
        public override ReflectionProbeMode mode { get { return planarReflectionProbe.mode; } }

        public override Matrix4x4 proxyToWorld { get { return planarReflectionProbe.proxyToWorld; } }
    }
}
