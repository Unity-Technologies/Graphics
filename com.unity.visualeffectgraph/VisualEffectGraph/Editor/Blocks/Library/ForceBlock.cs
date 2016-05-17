using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetForceConstant : VFXBlockType
    {
        public VFXBlockSetForceConstant()
        {
            Name = "Set Force (Constant)";
            Icon = "Force";
            Category = "Forces";

            Add(new VFXProperty(new VFXVectorType(new Vector3(0.0f,-9.81f,0.0f)),"Force"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += Force * deltaTime;";
        }
    }

    class VFXBlockSetForceRelative : VFXBlockType
    {
        public VFXBlockSetForceRelative()
        {
            Name = "Set Force (Relative)";
            Icon = "Force";
            Category = "Forces";

            Add(new VFXProperty(new VFXVectorType(new Vector3(1.0f,0.0f,0.0f)),"InfluenceSpeed"));
            Add(new VFXProperty(new VFXFloatType(1.0f),"DragCoefficient"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
float3 relativeForce = InfluenceSpeed - velocity;
velocity += InfluenceSpeed * (DragCoefficient * deltaTime);";
        }
    }

    class VFXBlockSetForceLinearDrag : VFXBlockType
    {
        public VFXBlockSetForceLinearDrag()
        {
            Name = "Apply Linear Drag (Constant)";
            Icon = "Drag";
            Category = "Forces";

            Add(new VFXProperty(new VFXFloatType(1.0f),"DragCoefficient"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity *= max(0.0,(1.0 - DragCoefficient * deltaTime));";
        }
    }

    class VFXBlockSetForceLinearDragOverLife : VFXBlockType
    {
        public VFXBlockSetForceLinearDragOverLife()
        {
            Name = "Apply Drag Over Life (Curve)";
            Icon = "Drag";
            Category = "Forces";

            Add(VFXProperty.Create<VFXCurveType>("DragCurve"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));
            Add(new VFXAttribute(CommonAttrib.Age, false));


            Source = @"
float ratio = saturate(age/lifetime);
float3 multiplier = SAMPLE(DragCurve,ratio);
velocity *= max(0.0,(1.0 - multiplier * deltaTime));";
        }
    }

    class VFXBlockSetForceConformToSphere : VFXBlockType
    {
        public VFXBlockSetForceConformToSphere()
        {
            Name = "Conform To Sphere";
            Icon = "Force";
            Category = "Forces";

            Add(new VFXProperty( new VFXSphereType(),"Sphere"));
            Add(new VFXProperty(new VFXFloatType(5.0f), "attractionSpeed"));
            Add(new VFXProperty(new VFXFloatType(20.0f), "attractionForce"));
            Add(new VFXProperty(new VFXFloatType(50.0f), "stickForce"));
            Add(new VFXProperty(new VFXFloatType(0.1f), "stickDistance"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));


            Source = @"
float3 dir = Sphere_center - position;
float distToCenter = length(dir);
float distToSurface = distToCenter - Sphere_radius;
dir /= distToCenter;
float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir;";
        }
    }

    class VFXBlockSetForceAttractor : VFXBlockType
    {
        public VFXBlockSetForceAttractor()
        {
            Name = "Attractor (Point)";
            Icon = "Force";
            Category = "Forces";

            Add(new VFXProperty( new VFXPositionType(),"Center"));
            Add(new VFXProperty(new VFXFloatType(5.0f), "AttractionForce"));
            Add(new VFXProperty(new VFXFloatType(0.05f), "AttractionOffset"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));


            Source = @"
float3 dir = Center - position;
float sqrDist = dot(dir,dir) + AttractionOffset;
velocity += normalize(dir) * (deltaTime * AttractionForce / sqrDist);";
        }
    }

    class VFXBlockVectorFieldForce : VFXBlockType
    {
        public VFXBlockVectorFieldForce()
        {
            Name = "Attractor (VectorField)";
            Icon = "Force";
            Category = "Forces";

            Add(new VFXProperty(new VFXTexture3DType(),"VectorField"));
            Add(new VFXProperty(new VFXTransformType(), "Transform"));
            Add(new VFXProperty(new VFXFloatType(1.0f), "Force"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Source = @"
float3 vectorFieldCoord = mul(Transform,position);
velocity += deltaTime * tex3Dlod(VectorField, float4(vectorFieldCoord, 0.0f)) * Force;";
        }

    }

}
