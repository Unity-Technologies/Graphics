using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXSDFCollision : VFXBlockType
    {
        public VFXSDFCollision()
        {
            Name = "Distance Field Collision";
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
float3 nextPos = position + velocity * deltaTime;
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

        //position -= velocity * deltaTime;
    }
}";
        }
    }

    class VFXSDFConformance : VFXBlockType
    {
        public VFXSDFConformance()
        {
            Name = "Distance Field Attractor";
            //Icon = "Force";
            Category = "Forces";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXTexture3DType>("DistanceField"));
            Add(new VFXProperty(new VFXOrientedBoxType(), "Box"));
            Add(new VFXProperty(new VFXFloatType(5.0f), "attractionSpeed"));
            Add(new VFXProperty(new VFXFloatType(20.0f), "attractionForce"));
            Add(new VFXProperty(new VFXFloatType(50.0f), "stickForce"));
            Add(new VFXProperty(new VFXFloatType(0.1f), "stickDistance"));

            Add(new VFXAttribute(CommonAttrib.Velocity, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));


            Source = @"
float3 tPos = mul(INVERSE(Box), float4(position,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleTexture(DistanceField, coord).x;

float3 absPos = abs(tPos);
float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
float3 dir;
if (outsideDist > 0.5f) // Check wether point is outside the box
{
    // in that case just move towards center
    dist += outsideDist - 0.5f;
    dir = normalize(float3(Box[0][3],Box[1][3],Box[2][3]) - position);
}
else
{
    // compute normal
    dir.x = SampleTexture(DistanceField, coord + float3(0.01,0,0)).x;
    dir.y = SampleTexture(DistanceField, coord + float3(0,0.01,0)).x;
    dir.z = SampleTexture(DistanceField, coord + float3(0,0,0.01)).x;
    dir = normalize((float3)dist - dir);
    if (dist < 0)
        dir = -dir;
    dir = normalize(mul(Box,float4(dir,0)));
}
  
float distToSurface = abs(dist); 

float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir;";
        }
    }

    class VFXSDFOrientation : VFXBlockType
    {
        public VFXSDFOrientation()
        {
            Name = "Distance Field Orientation";
            //Icon = "Force";
            Category = "Orient";
            CompatibleContexts = VFXContextDesc.Type.kAll;

            Add(VFXProperty.Create<VFXTexture3DType>("DistanceField"));
            Add(new VFXProperty(new VFXOrientedBoxType(), "Box"));
            Add(new VFXProperty(new VFXFloat3Type(new Vector3(0, 1, 0)), "Up"));

            Add(new VFXAttribute(CommonAttrib.Side, true));
            Add(new VFXAttribute(CommonAttrib.Up, true));
            Add(new VFXAttribute(CommonAttrib.Front, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));
            Add(new VFXAttribute(CommonAttrib.Velocity, false));


            Source = @"
float3 tPos = mul(INVERSE(Box), float4(position,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleTexture(DistanceField, coord).x;

float3 absPos = abs(tPos);
float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
float3 dir;
if (outsideDist > 0.5f) // Check wether point is outside the box
{
    // in that case just move towards center
    dist += outsideDist - 0.5f;
    dir = normalize(float3(Box[0][3],Box[1][3],Box[2][3]) - position);
}
else
{
    // compute normal
    dir.x = SampleTexture(DistanceField, coord + float3(0.01,0,0)).x;
    dir.y = SampleTexture(DistanceField, coord + float3(0,0.01,0)).x;
    dir.z = SampleTexture(DistanceField, coord + float3(0,0,0.01)).x;
    dir = normalize((float3)dist - dir);
    dir = normalize(mul(Box,float4(dir,0)));
}
  
front = dir;
side = normalize(cross(velocity,dir));
up = cross(dir,side);

float distToSurface = abs(dist);";
        }
    }

    class VFXSDFReveal : VFXBlockType
    {
        public VFXSDFReveal()
        {
            Name = "Distance Field Kill";
            //Icon = "Force";
            Category = "Test";
            CompatibleContexts = VFXContextDesc.Type.kAll;

            Add(VFXProperty.Create<VFXTexture3DType>("DistanceField"));
            Add(new VFXProperty(new VFXOrientedBoxType(), "Box"));
            Add(new VFXProperty(new VFXFloatType(), "Threshold"));

            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));

            Source = @"
float3 tPos = mul(INVERSE(Box), float4(position,1.0f)).xyz;
float3 coord = saturate(tPos + 0.5f);
float dist = SampleTexture(DistanceField, coord).x;
if (abs(dist) > Threshold)
    KILL;";
        }
    }
}
