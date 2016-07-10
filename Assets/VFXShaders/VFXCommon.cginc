// Pi variables are redefined here as UnityCG.cginc is not included for compute shader as it adds too many unused uniforms to constant buffers
#ifndef UNITY_CG_INCLUDED
#define UNITY_PI			3.14159265359f
#define UNITY_TWO_PI		6.28318530718f
#define UNITY_FOUR_PI		12.56637061436f
#define UNITY_INV_PI		0.31830988618f
#define UNITY_INV_TWO_PI	0.15915494309f							
#define UNITY_INV_FOUR_PI	0.07957747155f
#define UNITY_HALF_PI		1.57079632679f
#define UNITY_INV_HALF_PI	0.636619772367f
#endif

// Special semantics for VFX blocks
#define RAND rand(seed)
#define RAND2 float2(RAND,RAND)
#define RAND3 float3(RAND,RAND,RAND)
#define RAND4 float4(RAND,RAND,RAND,RAND)
#define KILL {kill = true;}
#define SAMPLE sampleSignal
#define INVERSE(m) Inv##m

// Macros to use Sphere semantic type directly
#define VFXPositionOnSphere(SphereName,cosPhi,theta,rNorm)	PositionOnSphere(SphereName##_center,SphereName##_radius,cosPhi,theta,rNorm)
#define VFXPositionOnSphereSurface(SphereName,cosPhi,theta)	PositionOnSphereSurface(SphereName##_center,SphereName##_radius,cosPhi,theta)

// center,radius: Sphere description
// cosPhi: cosine of Phi angle (we used the cosine directly as it used for uniform distribution)
// theta: theta angle
// rNorm: normalized radius in the sphere where the point lies
float3 PositionOnSphere(float3 center,float radius,float cosPhi,float theta,float rNorm)
{
	float2 sincosTheta;
	sincos(theta,sincosTheta.x,sincosTheta.y);
	sincosTheta *= sqrt(1.0 - cosPhi*cosPhi);
	return float3(sincosTheta,cosPhi) * (rNorm * radius) + center;	
}

float3 PositionOnSphereSurface(float3 center,float radius,float cosPhi,float theta)
{
	return PositionOnSphere(center,radius,cosPhi,theta,1.0f);
}

// Macros to use Cylinder semantic type directly
#define VFXPositionOnCylinder(CylinderName,hNorm,theta,rNorm)	PositionOnCylinder(CylinderName##_position,CylinderName##_direction,CylinderName##_height,CylinderName##_radius,hNorm,theta,rNorm)
#define VFXPositionOnCylinderSurface(CylinderName,hNorm,theta)	PositionOnCylinderSurface(CylinderName##_position,CylinderName##_direction,CylinderName##_height,CylinderName##_radius,hNorm,theta)

// pos,dir,height,radius: Cylinder description
// hNorm: normalized height for the point in [-0.5,0.5]
// theta: theta angle
// rNorm: normalise radius for the point
float3 PositionOnCylinder(float3 pos,float3 dir,float height,float radius,float hNorm,float theta,float rNorm)
{	
	float2 sincosTheta;
	sincos(theta,sincosTheta.x,sincosTheta.y);
	sincosTheta *= rNorm * radius;
	float3 normal = normalize(cross(dir,dir.zxy));
	float3 binormal = cross(normal,dir);
	return normal * sincosTheta.x + binormal * sincosTheta.y + dir * (hNorm * height) + pos;
}

float3 PositionOnCylinderSurface(float3 pos,float3 dir,float height,float radius,float hNorm,float theta)
{	
	return PositionOnCylinder(pos,dir,height,radius,hNorm,theta,1.0f);
}
