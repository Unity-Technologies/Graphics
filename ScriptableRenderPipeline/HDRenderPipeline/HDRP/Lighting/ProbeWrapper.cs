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
        public abstract Matrix4x4 GetCaptureToWorld(Camera viewCamera);
        public abstract Matrix4x4 GetCaptureProjection(Camera viewCamera);
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

        public override EnvShapeType proxyShapeType { get { return influenceShapeType; } }
        public override Vector3 proxyExtents { get { return influenceExtents; } }
        public override bool infiniteProjection { get { return probe.boxProjection == 0; } }
        public override Matrix4x4 proxyToWorld { get { return influenceToWorld; } }
        public override Matrix4x4 GetCaptureToWorld(Camera viewCamera) { return probe.localToWorld; }
        public override Matrix4x4 GetCaptureProjection(Camera viewCamera) { return Matrix4x4.identity; }
    }

    class PlanarReflectionProbeWrapper : ProbeWrapper
    {
        PlanarReflectionProbe probe;

        public PlanarReflectionProbeWrapper(PlanarReflectionProbe probe)
        {
            this.probe = probe;
        }

        public override Matrix4x4 influenceToWorld { get { return probe.influenceToWorld; } }
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

        public override Matrix4x4 proxyToWorld { get { return probe.proxyToWorld; } }
        public override Matrix4x4 GetCaptureToWorld(Camera viewCamera) { return probe.GetCaptureToWorld(viewCamera); }
        public override Matrix4x4 GetCaptureProjection(Camera viewCamera) {  return probe.GetCaptureProjection(viewCamera); }
    }
}
