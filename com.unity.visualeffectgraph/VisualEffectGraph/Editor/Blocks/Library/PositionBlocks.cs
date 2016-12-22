using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetPositionPoint : VFXBlockType
    {
        public VFXBlockSetPositionPoint()
        {
            Name = "Point";
            Icon = "Position";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXPositionType>("pos"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = pos;";
        }
    }

    class VFXBlockSetPositionMap : VFXBlockType
    {
        public VFXBlockSetPositionMap()
        {
            Name = "PositionMap Texture";
            Icon = "Position";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXTexture2DType>("tex"));
            Add(VFXProperty.Create<VFXOrientedBoxType>("box"));
            Add(VFXProperty.Create<VFXFloatType>("divergence"));
            Add(new VFXProperty(new VFXFloat3Type(new Vector3(0.5f, 0.5f, 0.5f)), "posmapcenter"));
            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float3 div = (RAND3 - 0.5f) * (divergence * 2.0f);
position = (div + SampleTexture(tex,RAND2).rgb) - posmapcenter;
position = mul(box,float4(position.xyz,1.0f)).xyz;"; 
        }
    }

    class VFXBlockSetPositionAABox : VFXBlockType
    {
        public VFXBlockSetPositionAABox()
        {
            Name = "Box (Axis-Aligned)";
            Icon = "Box";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXAABoxType>("aabox"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float3 minCoord = (aabox_size * -0.5f) + aabox_center;
position = (RAND3 * aabox_size) + minCoord;";
        }
    }

    class VFXBlockSetPositionBox : VFXBlockType
    {
        public VFXBlockSetPositionBox()
        {
            Name = "Box (Oriented)";
            Icon = "Box";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXOrientedBoxType>("box"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = RAND3 - 0.5f;
position = mul(box,float4(position,1.0f)).xyz;";
        }
    }

    class VFXBlockSetPositionSphereSurface : VFXBlockType
    {
        public VFXBlockSetPositionSphereSurface()
        {
            Name = "Sphere Surface";
            Icon = "Sphere";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
position = VFXPositionOnSphereSurface(Sphere,u1,u2);";
        }
    }

    class VFXBlockSetPositionSphereVolume : VFXBlockType
    {
        public VFXBlockSetPositionSphereVolume()
        {
            Name = "Sphere Volume";
            Icon = "Sphere";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float u3 = pow(RAND,1.0/3.0);
position = VFXPositionOnSphere(Sphere,u1,u2,u3);";
        }
    }

    class VFXBlockTransformPosition : VFXBlockType
    {
        public VFXBlockTransformPosition()
        {
            Name = "Transform";
            Icon = "Position";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXTransformType>("Transform"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = mul(Transform,float4(position,1.0f)).xyz;";
        }
    }

    class VFXBlockAnimatePositionCircular : VFXBlockType
    {
        public VFXBlockAnimatePositionCircular()
        {
            Name = "Animate (Circular)";
            Icon = "Disc";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXTransformType>("Transform"));
            Add(new VFXProperty(new VFXFloatType(1.0f), "Speed"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float2 sc;
sincos((totalTime/UNITY_PI)*Speed, sc.x,sc.y);
float3 pos = float3(sc.x, 0.0,sc.y);
position += mul(Transform,float4(pos,1.0f)).xyz;";
        }
    }

    class VFXBlockPositionCylinder : VFXBlockType
    {
        public VFXBlockPositionCylinder()
        {
            Name = "Cylinder Volume";
            Icon = "Cylinder";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXCylinderType>("Cylinder"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 1.0 * RAND - 0.5;
float u2 = UNITY_TWO_PI * RAND;
float u3 = sqrt(RAND);
position = VFXPositionOnCylinder(Cylinder,u1,u2,u3);";
        }
    }

    class VFXBlockPositionCylinderSurface : VFXBlockType
    {
        public VFXBlockPositionCylinderSurface()
        {
            Name = "Cylinder Surface";
            Icon = "Cylinder";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXCylinderType>("Cylinder"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 1.0 * RAND - 0.5;
float u2 = UNITY_TWO_PI * RAND;
position = VFXPositionOnCylinderSurface(Cylinder,u1,u2);";
        }
    }

    class VFXBlockPositionCylinderSurfaceSequence : VFXBlockType
    {
    public VFXBlockPositionCylinderSurfaceSequence()
        {
            Name = "Cylinder Surface Sequence";
            Icon = "Cylinder";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXCylinderType>("Cylinder"));
            Add(VFXProperty.Create<VFXFloatType>("RotationalRate"));
            Add(VFXProperty.Create<VFXFloatType>("LinearRate"));

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.ParticleId, false));

            Source = @"
float u1 = fmod(LinearRate * particleId,1.0f) - 0.5f;
float u2 = radians(RotationalRate * particleId);
position = VFXPositionOnCylinderSurface(Cylinder,u1,u2);";
        }
    }

    class VFXBlockPositionAABBSequence : VFXBlockType
    {
        public VFXBlockPositionAABBSequence()
        {
            Name = "AABB Sequence";
            Icon = "Box";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXAABoxType>("Box"));
            Add(new VFXProperty(new VFXFloat3Type(Vector3.one),"Number"));

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.ParticleId, false));

            Source = @"
float3 nPos;
nPos.x = fmod(particleId,Number.x);
nPos.y = fmod((int)(particleId / Number.x),Number.y);
nPos.z = fmod((int)(particleId / (Number.x * Number.y)),Number.z);
nPos = nPos / Number - 0.5f;
position = nPos * Box_size + Box_center;";
        }
    }

    class VFXBlockPositionSpline : VFXBlockType
    {
        public VFXBlockPositionSpline()
        {
            Name = "Bezier spline";
            Icon = "Curve";
            Category = "Position";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXSplineType>("Spline"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position += SAMPLE_SPLINE_POSITION(Spline,RAND);";
        }
    }
}
