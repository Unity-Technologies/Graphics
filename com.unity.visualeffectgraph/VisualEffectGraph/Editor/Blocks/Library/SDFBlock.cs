using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXSDFCollision : VFXBlockType
    {
        public VFXSDFCollision()
        {
            Name = "Distance Field";
            //Icon = "Sphere";
            Category = "Collision";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXTexture3DType>("DistanceField"));
            Add(new VFXProperty(new VFXOrientedBoxType(), "Box"));
            Add(new VFXProperty(new VFXFloatType(0.0f), "Elasticity"));
            Add(new VFXProperty(new VFXFloatType(0.0f), "Friction"));

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, true));


            Source = @"
float3 nextPos = position /*+ velocity * deltaTime*/;
float3 tPos = mul(INVERSE(Box), float4(nextPos,1.0f)).xyz;
float3 coord = tPos + 0.5f;
float dist = SampleTexture(DistanceField, coord).x;

if (dist <= 0.0f) // collision
{
    float3 n;
    n.x = SampleTexture(DistanceField, coord + float3(0.01,0,0)).x;
    n.y = SampleTexture(DistanceField, coord + float3(0,0.01,0)).x;
    n.z = SampleTexture(DistanceField, coord + float3(0,0,0.01)).x;
    n = normalize((float3)dist - n);

    tPos += n * dist; // push on boundaries
    
    // back in system space
    position = mul(Box,float4(tPos,1.0f)).xyz;
    n = normalize(mul(Box,float4(n,0)));    

    float projVelocity = dot(n,velocity);
	if (projVelocity > 0)
    {
        float3 nVelocity = projVelocity * n; // normal component
        float3 tVelocity = velocity - nVelocity; // tangential component

        velocity -= (1 + saturate(Elasticity)) * nVelocity;
        velocity -= saturate(Friction) * tVelocity;
    }
}";
        }
    }
}