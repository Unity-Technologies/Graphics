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

//#include "UnityCG.cginc"
//#include "UnityPBSLighting.cginc"
//#include "UnityDeferredLibrary.cginc"

#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"


#include "..\common\ShaderBase.h"
#include "LightDefinitions.cs"



uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mInvScrProjection;
uniform float4x4 g_mScrProjection;


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


float GetLinearDepth(float3 vP)
{
	Vec3 var = 1.0;
	//float4 v4Pres = mul(float4(vP,1.0), g_mInvScrProjection);
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

uint FetchLightCount(const uint tileOffs)
{
	return g_vLightList[ 16*tileOffs + 0]&0xffff;
}

uint FetchIndex(const uint tileOffs, const uint l)
{
	const uint l1 = l+1;
	return (g_vLightList[ 16*tileOffs + (l1>>1)]>>((l1&1)*16))&0xffff;
}

float3 ExecuteLightList(uint2 pixCoord, const uint offs);
float3 OverlayHeatMap(uint uNumLights, float3 c);


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

	pixCoord.y = (iHeight-1) - pixCoord.y;
	uint2 tileIDX = pixCoord / 16;

	const int offs = tileIDX.y*nrTilesX+tileIDX.x;

	
	float3 c = ExecuteLightList(pixCoord, offs);
	//c = OverlayHeatMap(FetchLightCount(offs), c);

	return float4(c,1.0);
	//return float4(pow(c,1/2.2),1.0);
}

struct UnityStandardData
{
	float3 specularColor;
	float3 diffuseColor;
	float3 normalWorld;
	float smoothness;
};

UnityStandardData UnityStandardDataFromGbuffer(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
	UnityStandardData data;

	data.normalWorld = normalize(2*gbuffer2.xyz-1);
	data.smoothness = gbuffer1.a;
	data.diffuseColor = gbuffer0.xyz; data.specularColor = gbuffer1.xyz;
	float ao = gbuffer0.a;

	return data;
}

float3 ExecuteLightList(uint2 pixCoord, const uint offs)
{
	float3 v3ScrPos = float3(pixCoord.x+0.5, pixCoord.y+0.5, FetchDepth(_CameraDepthTexture, pixCoord.xy).x);
	float linDepth = GetLinearDepth(v3ScrPos);
	float3 vP = GetViewPosFromLinDepth(v3ScrPos.xy, linDepth);

	float3 vWSpaceVDir = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);		//unity_CameraToWorld

	float4 gbuffer0 = _CameraGBufferTexture0.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer1 = _CameraGBufferTexture1.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer2 = _CameraGBufferTexture2.Load( uint3(pixCoord.xy, 0) );

	UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);


	float oneMinusReflectivity = 1.0 - SpecularStrength(data.specularColor.rgb);

	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;


	float3 ints = 0;

	const uint uNrLights = FetchLightCount(offs);
	
	uint l=0;

	// we need this outer loop for when we cannot assume a wavefront is 64 wide
	// since in this case we cannot assume the lights will remain sorted by type
	// during processing in lightlist_cs.hlsl
#if !defined(XBONE) && !defined(PLAYSTATION4)
	while(l<uNrLights)
#endif
	{
		uint uIndex = l<uNrLights ? FetchIndex(offs, l) : 0;
		uint uLgtType = l<uNrLights ? g_vLightData[uIndex].uLightType : 0;

		// specialized loop for spot lights
		while(l<uNrLights && uLgtType==SPOT_LIGHT)
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
			float d0=0.65, angularAtt = smoothstep(0.0, 1.0-d0, 1.0-length(cookCoord));
			[branch]if(bHasCookie)
			{
				cookCoord = cookCoord*0.5 + 0.5;
				angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, lgtDat.iSliceIndex), 0.0).w;
			}
			atten *= angularAtt*(fProjVec>0.0);                           // finally apply this to the dist att.
					

			UnityLight light;
			light.color.xyz = lgtDat.vCol.xyz*atten;
			light.dir.xyz = mul((float3x3) g_mViewToWorld, vL).xyz;		//unity_CameraToWorld
			light.ndotl = LambertTerm(data.normalWorld, light.dir.xyz);

			ints += UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind);
					

			++l; uIndex = l<uNrLights ? FetchIndex(offs, l) : 0;
			uLgtType = l<uNrLights ? g_vLightData[uIndex].uLightType : 0;
		}
		
		// specialized loop for sphere lights
		while(l<uNrLights && uLgtType==SPHERE_LIGHT)
		{
			SFiniteLightData lgtDat = g_vLightData[uIndex];	
			float3 vLp = lgtDat.vLpos.xyz;

			float3 toLight  = vLp - vP; 
			float dist = length(toLight);
			float3 vL = toLight / dist;
			float3 vLw = mul((float3x3) g_mViewToWorld, vL).xyz;		//unity_CameraToWorld

			float attLookUp = dist*lgtDat.fRecipRange; attLookUp *= attLookUp;
			float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

			const bool bHasCookie = (lgtDat.flags&HAS_COOKIE_TEXTURE)!=0;
			[branch]if(bHasCookie)
			{
				atten *= UNITY_SAMPLE_TEXCUBEARRAY_LOD(_pointCookieTextures, float4(-vLw, lgtDat.iSliceIndex), 0.0).w;
			}
					

			UnityLight light;
			light.color.xyz = lgtDat.vCol.xyz*atten;
			light.dir.xyz = vLw;
			light.ndotl = LambertTerm(data.normalWorld, vLw);
					
			ints += UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind);
					
			++l; uIndex = l<uNrLights ? FetchIndex(offs, l) : 0;
			uLgtType = l<uNrLights ? g_vLightData[uIndex].uLightType : 0;
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
