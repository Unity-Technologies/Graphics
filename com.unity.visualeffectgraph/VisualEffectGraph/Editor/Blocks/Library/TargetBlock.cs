using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetTargetPositionMap : VFXBlockType
    {
        public VFXBlockSetTargetPositionMap()
        {
            Name = "From Position Map";
            Icon = "Position";
            Category = "Target";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXOrientedBoxType>("Bounds"));
            Add(VFXProperty.Create<VFXTexture2DType>("PositionMap"));

            Add(new VFXAttribute("target", VFXValueType.kFloat3, true));

            Source = @"
float3 pos = SampleTexture(PositionMap,RAND2).rgb - 0.5f;
target = mul(Bounds, float4(pos,1.0)).xyz;";
        }
    }

    class VFXBlockAttractTarget : VFXBlockType
    {
        public VFXBlockAttractTarget()
        {
            Name = "Attract Particles";
            Icon = "Force";
            Category = "Target";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(new VFXProperty( new VFXFloatType(5.0f) ,"targetSpeed"));
            Add(new VFXProperty( new VFXFloatType(5.0f) ,"attractForce"));
            Add(new VFXProperty( new VFXFloatType(5.0f) ,"attractdistance"));

            Add(new VFXAttribute("target", VFXValueType.kFloat3, false));
            Add(new VFXAttribute(CommonAttrib.Position,false));
            Add(new VFXAttribute(CommonAttrib.Velocity,true));

            Source = @"
float3 delta = position - target;
float dist = sqrt(dot(delta,delta));
float t = saturate(dist / attractdistance);
float3 tgtVelocity = (-delta / dist) * (targetSpeed * t);
float3 deltaVelocity = tgtVelocity - velocity;
float deltaSpeed = length(deltaVelocity);
deltaVelocity /= deltaSpeed;
velocity += deltaVelocity * min(deltaSpeed, deltaTime * attractForce);";
        }
    }

}
