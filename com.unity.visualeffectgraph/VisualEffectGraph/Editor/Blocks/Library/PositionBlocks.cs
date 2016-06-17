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

            Add(VFXProperty.Create<VFXTexture2DType>("tex"));
            Add(VFXProperty.Create<VFXOrientedBoxType>("box"));
            Add(VFXProperty.Create<VFXFloatType>("divergence"));
            Add(new VFXProperty(new VFXVectorType(new Vector3(0.5f, 0.5f, 0.5f)), "posmapcenter"));
            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float3 div = (RAND3 - 0.5f) * (divergence * 2.0f);
position = (div + tex2Dlod(tex,float4(RAND2,0,0)).rgb) - posmapcenter;
position = mul(box,float4(position.xyz,1.0f)).xyz;"; 
        }
    }

    class VFXBlockSetPositionAABox : VFXBlockType
    {
        public VFXBlockSetPositionAABox()
        {
            Name = "Box (Axis-Aligned)";
            Icon = "Position";
            Category = "Position";

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
            Icon = "Position";
            Category = "Position";

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
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
position = (float3(sincosTheta,u1) * Sphere_radius) + Sphere_center;";
        }
    }

    class VFXBlockSetPositionSphereVolume : VFXBlockType
    {
        public VFXBlockSetPositionSphereVolume()
        {
            Name = "Sphere Volume";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float u3 = pow(RAND,1.0/3.0);
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
position = float3(sincosTheta,u1) * (u3 * Sphere_radius) + Sphere_center;";
        }
    }

    class VFXBlockTransformPosition : VFXBlockType
    {
        public VFXBlockTransformPosition()
        {
            Name = "Transform";
            Icon = "Position";
            Category = "Position";

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
            Icon = "Circle";
            Category = "Position";

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

            Add(VFXProperty.Create<VFXCylinderType>("Cylinder"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 1.0 * RAND - 0.5;
float u2 = UNITY_TWO_PI * RAND;
float u3 = sqrt(RAND);
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= u3 * Cylinder_radius;
float3 normal = normalize(cross(Cylinder_direction,Cylinder_direction.zxy));
float3 binormal = cross(normal,Cylinder_direction);
position = normal * sincosTheta.x + binormal * sincosTheta.y + Cylinder_direction * (u1 * Cylinder_height) + Cylinder_position;";
        }
    }

    class VFXBlockPositionCylinderSurface : VFXBlockType
    {
        public VFXBlockPositionCylinderSurface()
        {
            Name = "Cylinder Surface";
            Icon = "Cylinder";
            Category = "Position";

            Add(VFXProperty.Create<VFXCylinderType>("Cylinder"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 1.0 * RAND - 0.5;
float u2 = UNITY_TWO_PI * RAND;
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= Cylinder_radius;
float3 normal = normalize(cross(Cylinder_direction,Cylinder_direction.zxy));
float3 binormal = cross(normal,Cylinder_direction);
position = normal * sincosTheta.x + binormal * sincosTheta.y + Cylinder_direction * (u1 * Cylinder_height) + Cylinder_position;";
        }
    }
}
