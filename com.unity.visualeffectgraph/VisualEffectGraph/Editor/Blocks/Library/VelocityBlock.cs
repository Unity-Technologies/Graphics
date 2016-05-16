using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockVelocityConstant : VFXBlockType
    {
        public VFXBlockVelocityConstant()
        {
            Name = "Velocity (Constant)";
            Icon = "Velocity";
            Category = "Velocity";

            Add(VFXProperty.Create<VFXDirectionType>("Direction"));
            Add(new VFXProperty(new VFXFloatType(1.0f),"Speed"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity = Direction*Speed;";
        }
    }

    class VFXBlockVelocityRandomUniform : VFXBlockType
    {
        public VFXBlockVelocityRandomUniform()
        {
            Name = "Velocity (Random Uniform)";
            Icon = "Velocity";
            Category = "Velocity";

            Add(new VFXProperty(new VFXFloatType(1.0f),"Divergence"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += float3( (RAND*2-1) * Divergence,
                    (RAND*2-1) * Divergence,
                    (RAND*2-1) * Divergence);";
        }
    }

    class VFXBlockVelocityRandomVector : VFXBlockType
    {
        public VFXBlockVelocityRandomVector()
        {
            Name = "Velocity (Random Vector)";
            Icon = "Velocity";
            Category = "Velocity/Radial";

            Add(new VFXProperty(new VFXVectorType(new Vector3(0.25f,0.25f,0.5f)),"Divergence"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += float3( (RAND*2-1) * Divergence.x,
                    (RAND*2-1) * Divergence.y,
                    (RAND*2-1) * Divergence.z);";
        }
    }

    class VFXBlockVelocityRadial : VFXBlockType
    {
        public VFXBlockVelocityRadial()
        {
            Name = "Velocity (Radial)";
            Icon = "Velocity";
            Category = "Velocity/Radial";

            Add(new VFXProperty(new VFXFloat2Type(new Vector2(0f,180f)),"MinMaxAngle"));
            Add(new VFXProperty(new VFXFloat2Type(new Vector2(0f,0.5f)),"MinMaxSpeed"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
float2 z = cos(radians(MinMaxAngle));
float u1 = lerp(z.x,z.y,RAND);
float u2 = UNITY_TWO_PI * RAND;
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
velocity += float3(sincosTheta,u1).xzy * lerp(MinMaxSpeed.x,MinMaxSpeed.y,RAND);";
        }
    }
}
