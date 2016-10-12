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

struct VFXSampler2D
{
	Texture2D t;
	SamplerState s;
};

struct VFXSampler3D
{
	Texture3D t;
	SamplerState s;
};

// indices to access to system data
#define VFX_DATA_UPDATE_ARG_GROUP_X 	0
#define VFX_DATA_RENDER_ARG_NB_INDEX 	4
#define VFX_DATA_RENDER_ARG_NB_INSTANCE	5
#define VFX_DATA_NB_CURRENT 			8
#define VFX_DATA_NB_INIT				9
#define VFX_DATA_NB_UPDATE 				10
#define VFX_DATA_NB_FREE				11

#ifdef VFX_WORLD_SPACE // World Space
float3 VFXCameraPos() 					{ return _WorldSpaceCameraPos.xyz; }
float3 VFXCameraLook()					{ return -unity_WorldToCamera[2].xyz; }
float4x4 VFXCameraMatrix()				{ return unity_WorldToCamera; }
float VFXNearPlaneDist(float3 position) { return mul(unity_WorldToCamera,float4(position,1.0f)).z; } 
#elif defined(VFX_LOCAL_SPACE) // Local space
float3 VFXCameraPos() 					{ return mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; }
float3 VFXCameraLook()					{ return UNITY_MATRIX_MV[2].xyz; }
float4x4 VFXCameraMatrix()				{ return UNITY_MATRIX_MV; }
float VFXNearPlaneDist(float3 position) { return -mul(UNITY_MATRIX_MV,float4(position,1.0f)).z; }
#endif

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

float4 SampleTexture(VFXSampler2D s,float2 coords)
{
	return s.t.SampleLevel(s.s,coords,0.0f);
}

float4 SampleTexture(VFXSampler3D s,float3 coords)
{
	return s.t.SampleLevel(s.s,coords,0.0f);
}

VFXSampler2D InitSampler(Texture2D t,SamplerState s)
{
	VFXSampler2D vfxSampler;
	vfxSampler.t = t;
	vfxSampler.s = s;
	return vfxSampler;
}

VFXSampler3D InitSampler(Texture3D t,SamplerState s)
{
	VFXSampler3D vfxSampler;
	vfxSampler.t = t;
	vfxSampler.s = s;
	return vfxSampler;
}

uint ConvertFloatToSortableUint(float f)
{
	int mask = (-(int)(asuint(f) >> 31)) | 0x80000000;
	return asuint(f) ^ mask;
}

uint3 ConvertFloatToSortableUint(float3 f)
{
	uint3 res;
	res.x = ConvertFloatToSortableUint(f.x);
	res.y = ConvertFloatToSortableUint(f.y);
	res.z = ConvertFloatToSortableUint(f.z);
	return res;
}
