Shader "Hidden/Internal-Obscurity" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_ShadowMapTexture ("", any) = "" {}
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {




Pass 
{
	ZWrite Off
	ZTest Always
	Cull Off
	Blend Off
	//Blend [_SrcBlend] [_DstBlend]
	

CGPROGRAM
#pragma target 5.0
#pragma vertex vert
#pragma fragment frag
//#pragma multi_compile USE_FPTL_LIGHTLIST	USE_CLUSTERED_LIGHTLIST

//#define ENABLE_DEPTH_TEXTURE_BACKPLANE
//#define USE_CLUSTERED_LIGHTLIST

#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"

#include "..\common\ShaderBase.h"
#include "LightDefinitions.cs.hlsl"



uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mInvScrProjection;
uniform float4x4 g_mScrProjection;
uniform uint g_nDirLights;

//---------------------------------------------------------------------------------------------------------------------------------------------------------
// TODO:  clean up.. -va
#define MAX_SHADOW_LIGHTS 10
#define MAX_SHADOWMAP_PER_LIGHT 6
#define MAX_DIRECTIONAL_SPLIT  4

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

CBUFFER_START(ShadowLightData)

float4 g_vShadow3x3PCFTerms0;
float4 g_vShadow3x3PCFTerms1;
float4 g_vShadow3x3PCFTerms2;
float4 g_vShadow3x3PCFTerms3;

float4 g_vDirShadowSplitSpheres[MAX_DIRECTIONAL_SPLIT];
float4x4 g_matWorldToShadow[MAX_SHADOW_LIGHTS * MAX_SHADOWMAP_PER_LIGHT];

CBUFFER_END
//---------------------------------------------------------------------------------------------------------------------------------------------------------


Texture2D _CameraDepthTexture;
Texture2D _CameraGBufferTexture0;
Texture2D _CameraGBufferTexture1;
Texture2D _CameraGBufferTexture2;
//UNITY_DECLARE_TEX2D(_LightTextureB0);
sampler2D _LightTextureB0;
UNITY_DECLARE_TEX2DARRAY(_spotCookieTextures);
UNITY_DECLARE_TEXCUBEARRAY(_pointCookieTextures);
		
StructuredBuffer<uint> g_vLightList;
StructuredBuffer<SFiniteLightData> g_vLightData;
StructuredBuffer<DirectionalLight> g_dirLightData;



float GetLinearDepth(float zDptBufSpace)	// 0 is near 1 is far
{
	float3 vP = float3(0.0f,0.0f,zDptBufSpace);
	float4 v4Pres = mul(g_mInvScrProjection, float4(vP,1.0));
	return v4Pres.z / v4Pres.w;
}


float3 GetViewPosFromLinDepth(float2 v2ScrPos, float fLinDepth)
{
	float fSx = g_mScrProjection[0].x;
	//float fCx = g_mScrProjection[2].x;
	float fCx = g_mScrProjection[0].z;
	float fSy = g_mScrProjection[1].y;
	//float fCy = g_mScrProjection[2].y;
	float fCy = g_mScrProjection[1].z;	

#ifdef LEFT_HAND_COORDINATES
	return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
#else
	return fLinDepth*float3( -((v2ScrPos.x+fCx)/fSx), -((v2ScrPos.y+fCy)/fSy), 1.0 );
#endif
}

#ifdef USE_CLUSTERED_LIGHTLIST

uniform float g_fClustScale;
uniform float g_fClustBase;
uniform float g_fNearPlane;
uniform float g_fFarPlane;
//uniform int	  g_iLog2NumClusters;		// numClusters = (1<<g_iLog2NumClusters)
uniform float g_fLog2NumClusters;
static int g_iLog2NumClusters;

Buffer<uint> g_vLayeredOffsetsBuffer;
#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
Buffer<float> g_logBaseBuffer;
#endif

#include "ClusteredUtils.h"

void GetLightCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth)
{
	g_iLog2NumClusters = (int) (g_fLog2NumClusters+0.5);		// ridiculous

#ifdef ENABLE_DEPTH_TEXTURE_BACKPLANE
	float logBase = g_logBaseBuffer[tileIDX.y*nrTilesX + tileIDX.x];
#else
	float logBase = g_fClustBase;
#endif
	int clustIdx = SnapToClusterIdx(linDepth, logBase);

	int nrClusters = (1<<g_iLog2NumClusters);
	const int idx = ((DIRECT_LIGHT*nrClusters + clustIdx)*nrTilesY + tileIDX.y)*nrTilesX + tileIDX.x;
	uint dataPair = g_vLayeredOffsetsBuffer[idx];
	uStart = dataPair&0x7ffffff;
	uNrLights = (dataPair>>27)&31;
}

uint FetchIndex(const uint tileOffs, const uint l)
{
	return g_vLightList[ tileOffs+l ];
}

#else

void GetLightCountAndStart(out uint uStart, out uint uNrLights, uint2 tileIDX, int nrTilesX, int nrTilesY, float linDepth)
{
	const int tileOffs = (tileIDX.y+DIRECT_LIGHT*nrTilesY)*nrTilesX+tileIDX.x;

	uNrLights = g_vLightList[ 16*tileOffs + 0]&0xffff;
	uStart = tileOffs;
}

uint FetchIndex(const uint tileOffs, const uint l)
{
	const uint l1 = l+1;
	return (g_vLightList[ 16*tileOffs + (l1>>1)]>>((l1&1)*16))&0xffff;
}

#endif

float3 ExecuteLightList(uint2 pixCoord, uint start, uint numLights, float linDepth);
float3 OverlayHeatMap(uint uNumLights, float3 c);

#define VALVE_DECLARE_SHADOWMAP( tex ) Texture2D tex; SamplerComparisonState sampler##tex
#define VALVE_SAMPLE_SHADOW( tex, coord ) tex.SampleCmpLevelZero( sampler##tex, (coord).xy, (coord).z )

VALVE_DECLARE_SHADOWMAP(g_tShadowBuffer);

float ComputeShadow_PCF_3x3_Gaussian(float3 vPositionWs, float4x4 matWorldToShadow)
{
	float4 vPositionTextureSpace = mul(float4(vPositionWs.xyz, 1.0), matWorldToShadow);
	vPositionTextureSpace.xyz /= vPositionTextureSpace.w;

	float2 shadowMapCenter = vPositionTextureSpace.xy;

	if ((shadowMapCenter.x < 0.0f) || (shadowMapCenter.x > 1.0f) || (shadowMapCenter.y < 0.0f) || (shadowMapCenter.y > 1.0f))
		return 1.0f;

	float objDepth = saturate(257.0 / 256.0 - vPositionTextureSpace.z);

	float4 v20Taps;
	v20Taps.x = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.xy, objDepth)).x; //  1  1
	v20Taps.y = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.zy, objDepth)).x; // -1  1
	v20Taps.z = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.xw, objDepth)).x; //  1 -1
	v20Taps.w = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.zw, objDepth)).x; // -1 -1
	float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
	if ((flSum == 0.0) || (flSum == 1.0))
		return flSum;
	flSum *= g_vShadow3x3PCFTerms0.x * 4.0;

	float4 v33Taps;
	v33Taps.x = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms2.xz, objDepth)).x; //  1  0
	v33Taps.y = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms3.xz, objDepth)).x; // -1  0
	v33Taps.z = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms3.zy, objDepth)).x; //  0 -1
	v33Taps.w = VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms2.zy, objDepth)).x; //  0  1
	flSum += dot(v33Taps.xyzw, g_vShadow3x3PCFTerms0.yyyy);

	flSum += VALVE_SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy, objDepth)).x * g_vShadow3x3PCFTerms0.z;

	return flSum;
}

//---------------------------------------------------------------------------------------------------------------------------------------------------------
/**
* Gets the cascade weights based on the world position of the fragment and the positions of the split spheres for each cascade.
* Returns an invalid split index if past shadowDistance (ie 4 is invalid for cascade)
*/
float GetSplitSphereIndexForDirshadows(float3 wpos)
{
	float3 fromCenter0 = wpos.xyz - g_vDirShadowSplitSpheres[0].xyz;
	float3 fromCenter1 = wpos.xyz - g_vDirShadowSplitSpheres[1].xyz;
	float3 fromCenter2 = wpos.xyz - g_vDirShadowSplitSpheres[2].xyz;
	float3 fromCenter3 = wpos.xyz - g_vDirShadowSplitSpheres[3].xyz;
	float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

	float4 vDirShadowSplitSphereSqRadii;
	vDirShadowSplitSphereSqRadii.x = g_vDirShadowSplitSpheres[0].w;
	vDirShadowSplitSphereSqRadii.y = g_vDirShadowSplitSpheres[1].w;
	vDirShadowSplitSphereSqRadii.z = g_vDirShadowSplitSpheres[2].w;
	vDirShadowSplitSphereSqRadii.w = g_vDirShadowSplitSpheres[3].w;
	fixed4 weights = float4(distances2 < vDirShadowSplitSphereSqRadii);
	weights.yzw = saturate(weights.yzw - weights.xyz);
	return 4 - dot(weights, float4(4, 3, 2, 1));
}

float SampleShadow(uint type, float3 vPositionWs, float3 vPositionToLightDirWs, uint lightIndex)
{
	float flShadowScalar = 1.0;
	int shadowSplitIndex = 0;

	if (type == DIRECTIONAL_LIGHT)
	{
		shadowSplitIndex = GetSplitSphereIndexForDirshadows(vPositionWs);
	}

	else if (type == SPHERE_LIGHT)
	{
		float3 absPos = abs(vPositionToLightDirWs);
		shadowSplitIndex = (vPositionToLightDirWs.z > 0) ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;
		if (absPos.x > absPos.y)
		{
			if (absPos.x > absPos.z)
			{
				shadowSplitIndex = (vPositionToLightDirWs.x > 0) ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
			}
		}
		else
		{
			if (absPos.y > absPos.z)
			{
				shadowSplitIndex = (vPositionToLightDirWs.y > 0) ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
			}
		}
	}

	flShadowScalar = ComputeShadow_PCF_3x3_Gaussian(vPositionWs.xyz, g_matWorldToShadow[lightIndex * MAX_SHADOWMAP_PER_LIGHT + shadowSplitIndex]);
	return flShadowScalar;
}

struct v2f {
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(vertex);
	o.texcoord = texcoord.xy;
	return o;
}

half4 frag (v2f i) : SV_Target
{
	uint2 pixCoord = ((uint2) i.vertex.xy);

	uint iWidth;
	uint iHeight;
	_CameraDepthTexture.GetDimensions(iWidth, iHeight);
	uint nrTilesX = (iWidth+15)/16;
	uint nrTilesY = (iHeight+15)/16;

	uint2 tileIDX = pixCoord / 16;

	float zbufDpth = FetchDepth(_CameraDepthTexture, pixCoord.xy).x;
	float linDepth = GetLinearDepth(zbufDpth);

	uint numLights=0, start=0;
	GetLightCountAndStart(start, numLights, tileIDX, nrTilesX, nrTilesY, linDepth);

	float3 c = ExecuteLightList(pixCoord, start, numLights, linDepth);
	//c = OverlayHeatMap(numLights, c);
	return float4(c,1.0);
}

struct StandardData
{
	float3 specularColor;
	float3 diffuseColor;
	float3 normalWorld;
	float smoothness;
};

StandardData UnityStandardDataFromGbuffer(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
	StandardData data;

	data.normalWorld = normalize(2*gbuffer2.xyz-1);
	data.smoothness = gbuffer1.a;
	data.diffuseColor = gbuffer0.xyz; data.specularColor = gbuffer1.xyz;
	float ao = gbuffer0.a;

	return data;
}

float3 ExecuteLightList(uint2 pixCoord, uint start, uint numLights, float linDepth)
{
	float3 vP = GetViewPosFromLinDepth(float2(pixCoord.x+0.5, pixCoord.y+0.5), linDepth);
	float3 vWSpaceVDir = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);		//unity_CameraToWorld

	float4 gbuffer0 = _CameraGBufferTexture0.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer1 = _CameraGBufferTexture1.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer2 = _CameraGBufferTexture2.Load( uint3(pixCoord.xy, 0) );

	StandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);


	float oneMinusReflectivity = 1.0 - SpecularStrength(data.specularColor.rgb);

	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;


	float3 ints = 0;
	
	uint l=0;

	float3 vPositionWs = mul(g_mViewToWorld, float4(vP, 1));

	for (int i = 0; i < g_nDirLights; i++)
	{
		DirectionalLight lightData = g_dirLightData[i];
		float atten = 1;

		[branch]
		if (lightData.uShadowLightIndex != 0xffffffff)
		{
			float shadowScalar = SampleShadow(DIRECTIONAL_LIGHT, vPositionWs, 0, lightData.uShadowLightIndex);
			atten *= shadowScalar;
		}

		UnityLight light;
		light.color.xyz = lightData.vCol.xyz * atten;
		light.dir.xyz = mul((float3x3) g_mViewToWorld, -lightData.vLaxisZ).xyz;

		ints += UNITY_BRDF_PBS(data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind);
	}

	// we need this outer loop for when we cannot assume a wavefront is 64 wide
	// since in this case we cannot assume the lights will remain sorted by type
	// during processing in lightlist_cs.hlsl
#if !defined(XBONE) && !defined(PLAYSTATION4)
	while(l<numLights)
#endif
	{
		uint uIndex = l<numLights ? FetchIndex(start, l) : 0;
		uint uLgtType = l<numLights ? g_vLightData[uIndex].uLightType : 0;

		// specialized loop for spot lights
		while(l<numLights && uLgtType==SPOT_LIGHT)
		{
			SFiniteLightData lgtDat = g_vLightData[uIndex];	
			float3 vLp = lgtDat.vLpos.xyz;

			float3 toLight  = vLp - vP;
			float dist = length(toLight);
			float3 vL = toLight / dist;
			
			float attLookUp = dist*lgtDat.fRecipRange; attLookUp *= attLookUp;
			float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;
					
			// spot attenuation
			const float fProjVec = -dot(vL, lgtDat.vLaxisZ.xyz);		// spotDir = lgtDat.vLaxisZ.xyz
			float2 cookCoord = (-lgtDat.cotan)*float2( dot(vL, lgtDat.vLaxisX.xyz), dot(vL, lgtDat.vLaxisY.xyz) ) / fProjVec;

			const bool bHasCookie = (lgtDat.flags&IS_CIRCULAR_SPOT_SHAPE)==0;		// all square spots have cookies
			float d0 = 0.65;
			float4 angularAtt = float4(1,1,1,smoothstep(0.0, 1.0-d0, 1.0-length(cookCoord)));
			[branch]if(bHasCookie)
			{
				cookCoord = cookCoord*0.5 + 0.5;
				angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, lgtDat.iSliceIndex), 0.0);
			}
			atten *= angularAtt.w*(fProjVec>0.0);                           // finally apply this to the dist att.
			
			const bool bHasShadow = (lgtDat.flags&HAS_SHADOW)!=0;
			[branch]if(bHasShadow)
			{
				float shadowScalar = SampleShadow(SPOT_LIGHT, vPositionWs, 0, lgtDat.uShadowLightIndex);
				atten *= shadowScalar;
			}

			UnityLight light;
			light.color.xyz = lgtDat.vCol.xyz*atten*angularAtt.xyz;
			light.dir.xyz = mul((float3x3) g_mViewToWorld, vL).xyz;		//unity_CameraToWorld

			ints += UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind);

			++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
			uLgtType = l<numLights ? g_vLightData[uIndex].uLightType : 0;
		}
		
		// specialized loop for sphere lights
		while(l<numLights && uLgtType==SPHERE_LIGHT)
		{
			SFiniteLightData lgtDat = g_vLightData[uIndex];	
			float3 vLp = lgtDat.vLpos.xyz;

			float3 toLight  = vLp - vP; 
			float dist = length(toLight);
			float3 vL = toLight / dist;
			float3 vLw = mul((float3x3) g_mViewToWorld, vL).xyz;		//unity_CameraToWorld

			float attLookUp = dist*lgtDat.fRecipRange; attLookUp *= attLookUp;
			float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

			float4 cookieColor = float4(1,1,1,1);
			
			const bool bHasCookie = (lgtDat.flags&HAS_COOKIE_TEXTURE)!=0;
			[branch]if(bHasCookie)
			{
				float3 cookieCoord = -float3(dot(vL, lgtDat.vLaxisX.xyz), dot(vL, lgtDat.vLaxisY.xyz), dot(vL, lgtDat.vLaxisZ.xyz));	// negate to make vL a fromLight vector
				cookieColor = UNITY_SAMPLE_TEXCUBEARRAY_LOD(_pointCookieTextures, float4(cookieCoord, lgtDat.iSliceIndex), 0.0);
				atten *= cookieColor.w;
			}
			
			const bool bHasShadow = (lgtDat.flags&HAS_SHADOW)!=0;
			[branch]if(bHasShadow)
			{
				float shadowScalar = SampleShadow(SPHERE_LIGHT, vPositionWs, vLw, lgtDat.uShadowLightIndex);
				atten *= shadowScalar;
			}

			UnityLight light;
			light.color.xyz = lgtDat.vCol.xyz*atten*cookieColor.xyz;
			light.dir.xyz = vLw;
					
			ints += UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind);
					
			++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
			uLgtType = l<numLights ? g_vLightData[uIndex].uLightType : 0;
		}

#if !defined(XBONE) && !defined(PLAYSTATION4)
		//if(uLgtType>=MAX_TYPES) ++l;
		if(uLgtType!=SPOT_LIGHT && uLgtType!=SPHERE_LIGHT) ++l;
#endif
	}

	return ints;
}

float3 OverlayHeatMap(uint uNumLights, float3 c)
{
	/////////////////////////////////////////////////////////////////////
	//
	const float4 kRadarColors[12] = 
	{
		float4(0.0,0.0,0.0,0.0),   // black
		float4(0.0,0.0,0.6,0.5),   // dark blue
		float4(0.0,0.0,0.9,0.5),   // blue
		float4(0.0,0.6,0.9,0.5),   // light blue
		float4(0.0,0.9,0.9,0.5),   // cyan
		float4(0.0,0.9,0.6,0.5),   // blueish green
		float4(0.0,0.9,0.0,0.5),   // green
		float4(0.6,0.9,0.0,0.5),   // yellowish green
		float4(0.9,0.9,0.0,0.5),   // yellow
		float4(0.9,0.6,0.0,0.5),   // orange
		float4(0.9,0.0,0.0,0.5),   // red
		float4(1.0,0.0,0.0,0.9)    // strong red
	};

	float fMaxNrLightsPerTile = 24;



	int nColorIndex = uNumLights==0 ? 0 : (1 + (int) floor(10 * (log2((float)uNumLights) / log2(fMaxNrLightsPerTile))) );
	nColorIndex = nColorIndex<0 ? 0 : nColorIndex;
	float4 col = nColorIndex>11 ? float4(1.0,1.0,1.0,1.0) : kRadarColors[nColorIndex];

	return lerp(c, pow(col.xyz, 2.2), 0.3*col.w);
}

ENDCG 
}

}
Fallback Off
}
