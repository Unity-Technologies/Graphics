using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockVelocityConstant : VFXBlockType
    {
        public VFXBlockVelocityConstant()
        {
            Name = "Constant";
            Icon = "Velocity";
            Category = "Velocity";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXVectorType>("Velocity"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += Velocity;";
        }
    }

    class VFXBlockVelocityRandomUniform : VFXBlockType
    {
        public VFXBlockVelocityRandomUniform()
        {
            Name = "Random Uniform";
            Icon = "Velocity";
            Category = "Velocity";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(new VFXProperty(new VFXFloatType(1.0f),"Divergence"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += (RAND3 * 2 - 1) * Divergence;";
        }
    }

    class VFXBlockVelocityRandomVector : VFXBlockType
    {
        public VFXBlockVelocityRandomVector()
        {
            Name = "Random Vector";
            Icon = "Velocity";
            Category = "Velocity/Radial";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(new VFXProperty(new VFXVectorType(new Vector3(0.25f,0.25f,0.5f)),"Divergence"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
velocity += (RAND3 * 2 - 1) * Divergence;";
        }
    }

    class VFXBlockVelocityRadial : VFXBlockType
    {
        public VFXBlockVelocityRadial()
        {
            Name = "By Angle Uniform";
            Icon = "Velocity";
            Category = "Velocity/Radial";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

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

    class VFXBlockVelocityRadialFromOrigin : VFXBlockType
    {
        public VFXBlockVelocityRadialFromOrigin()
        {
            Name = "From Origin";
            Icon = "Velocity";
            Category = "Velocity/Radial";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXPositionType>("Origin"));
            Add(new VFXProperty(new VFXFloat2Type(new Vector2(0f,0.5f)),"MinMaxSpeed"));

            Add(new VFXAttribute(CommonAttrib.Position, false));
            Add(new VFXAttribute(CommonAttrib.Velocity, true));

            Source = @"
float3 dir = normalize(position - Origin);
float speed = lerp(MinMaxSpeed.x,MinMaxSpeed.y,RAND);
velocity += dir * speed;";
        }
    }
}
