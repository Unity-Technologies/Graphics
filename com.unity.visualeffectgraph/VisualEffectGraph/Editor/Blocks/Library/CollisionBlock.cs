using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXBlockCollideWithPlane : VFXBlockType
    {
        public VFXBlockCollideWithPlane()
        {
            Name = "Infinite Plane";
            Icon = "Position";
            Category = "Collision";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXPlaneType>("Plane"));
            Add(new VFXProperty(new VFXFloatType(0.66666f), "Elasticity"));

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, true));


            Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = Plane_position - nextPos;
float distToPlane = dot(dir,Plane_normal);
if (distToPlane >= 0)
{
	float projVelocity = dot(Plane_normal,velocity);
	if (projVelocity < 0)
		velocity -= ((1 + Elasticity) * projVelocity) * Plane_normal;

	position += Plane_normal * distToPlane;
}";
        }
    }

    public class VFXBlockCollideWithSphere : VFXBlockType
    {
        public VFXBlockCollideWithSphere()
        {
            Name = "Sphere";
            Icon = "Sphere";
            Category = "Collision";
            CompatibleContexts = VFXContextDesc.Type.kInitAndUpdate;

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));
            Add(new VFXProperty(new VFXFloatType(0.66666f), "Elasticity"));

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, true));


            Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = Sphere_center - nextPos;
float sqrLength = dot(dir,dir);
if (sqrLength <= Sphere_radius * Sphere_radius)
{	
	float dist = sqrt(sqrLength);
	float3 n = dir / dist;	
	float projVelocity = dot(n,velocity);
	
	if (projVelocity > 0)
		velocity -= ((1 + Elasticity) * projVelocity) * n;
		
	position += n * (dist - Sphere_radius);
}";
        }
    }

}
