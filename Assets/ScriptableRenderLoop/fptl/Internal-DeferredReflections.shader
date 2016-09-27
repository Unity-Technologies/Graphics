Shader "Hidden/Internal-TiledReflections" {
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
	//Blend Off
	Blend [_SrcBlend] [_DstBlend]
	

CGPROGRAM
#pragma target 5.0
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"


#include "..\common\ShaderBase.h"
#include "LightDefinitions.cs"



uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mWorldToView;
uniform float4x4 g_mInvScrProjection;
uniform float4x4 g_mScrProjection;

Texture2D _CameraDepthTexture;
Texture2D _CameraGBufferTexture0;
Texture2D _CameraGBufferTexture1;
Texture2D _CameraGBufferTexture2;

UNITY_DECLARE_TEXCUBEARRAY(_reflCubeTextures);

StructuredBuffer<uint> g_vLightList;
StructuredBuffer<SFiniteLightData> g_vLightData;


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

uint FetchLightCount(const uint tileOffs)
{
	return g_vLightList[ 16*tileOffs + 0]&0xffff;
}

uint FetchIndex(const uint tileOffs, const uint l)
{
	const uint l1 = l+1;
	return (g_vLightList[ 16*tileOffs + (l1>>1)]>>((l1&1)*16))&0xffff;
}

float3 ExecuteReflectionProbes(uint2 pixCoord, const uint offs);
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

	uint2 tileIDX = pixCoord / 16;

	const int offs = tileIDX.y*nrTilesX+tileIDX.x + nrTilesX*nrTilesY;		// offset to where the reflection probes are

	
	float3 c = ExecuteReflectionProbes(pixCoord, offs);
	//c = OverlayHeatMap(FetchLightCount(offs), c);

	return float4(c,1.0);
}

struct StandardData
{
	float3 specularColor;
	float3 diffuseColor;
	float3 normalWorld;
	float smoothness;
	float occlusion;
};

StandardData UnityStandardDataFromGbuffer(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2)
{
	StandardData data;

	data.normalWorld = normalize(2*gbuffer2.xyz-1);
	data.smoothness = gbuffer1.a;
	data.diffuseColor = gbuffer0.xyz; data.specularColor = gbuffer1.xyz;
	data.occlusion = gbuffer0.a;

	return data;
}

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
	return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBEARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn);

float3 ExecuteReflectionProbes(uint2 pixCoord, const uint offs)
{
	float3 v3ScrPos = float3(pixCoord.x+0.5, pixCoord.y+0.5, FetchDepth(_CameraDepthTexture, pixCoord.xy).x);
	float linDepth = GetLinearDepth(v3ScrPos.z);
	float3 vP = GetViewPosFromLinDepth(v3ScrPos.xy, linDepth);
	float3 worldPos = mul(g_mViewToWorld, float4(vP.xyz,1.0)).xyz;		//unity_CameraToWorld

	float3 vWSpaceVDir = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);		//unity_CameraToWorld

	float4 gbuffer0 = _CameraGBufferTexture0.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer1 = _CameraGBufferTexture1.Load( uint3(pixCoord.xy, 0) );
	float4 gbuffer2 = _CameraGBufferTexture2.Load( uint3(pixCoord.xy, 0) );

	StandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
	
	float oneMinusReflectivity = 1.0 - SpecularStrength(data.specularColor.rgb);
	float3 worldNormalRefl = reflect(-vWSpaceVDir, data.normalWorld);

	float3 vspaceRefl = mul((float3x3) g_mWorldToView, worldNormalRefl).xyz;


	UnityLight light;
	light.color = 0;
	light.dir = 0;
	
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
		
		// specialized loop for sphere lights
		while(l<uNrLights && uLgtType==(uint) BOX_LIGHT)
		{
			SFiniteLightData lgtDat = g_vLightData[uIndex];	
			float3 vLp = lgtDat.vLpos.xyz;
			float3 vecToSurfPos  = vP - vLp;		// vector from reflection volume to surface position in camera space
			float3 posInReflVolumeSpace = float3( dot(vecToSurfPos, lgtDat.vLaxisX), dot(vecToSurfPos, lgtDat.vLaxisY), dot(vecToSurfPos, lgtDat.vLaxisZ) );


			float blendDistance = lgtDat.fProbeBlendDistance;//unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe

			float3 sampleDir;
			if((lgtDat.flags&IS_BOX_PROJECTED)!=0)
			{
				// For box projection, use expanded bounds as they are rendered; otherwise
				// box projection artifacts when outside of the box.
				//float4 boxMin = unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
				//float4 boxMax = unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
				//sampleDir = BoxProjectedCubemapDirection (worldNormalRefl, worldPos, unity_SpecCube0_ProbePosition, boxMin, boxMax);

				float4 vBoxOuterDistance = float4( lgtDat.vBoxInnerDist + float3(blendDistance, blendDistance, blendDistance), 0.0 );
#if 0
				// if rotation is NOT supported
				sampleDir = BoxProjectedCubemapDirection(worldNormalRefl, posInReflVolumeSpace, float4(lgtDat.vLocalCubeCapturePoint, 1.0), -vBoxOuterDistance, vBoxOuterDistance);
#else
				float3 volumeSpaceRefl = float3( dot(vspaceRefl, lgtDat.vLaxisX), dot(vspaceRefl, lgtDat.vLaxisY), dot(vspaceRefl, lgtDat.vLaxisZ) );
				float3 vPR = BoxProjectedCubemapDirection(volumeSpaceRefl, posInReflVolumeSpace, float4(lgtDat.vLocalCubeCapturePoint, 1.0), -vBoxOuterDistance, vBoxOuterDistance);	// Volume space corrected reflection vector
				sampleDir = mul( (float3x3) g_mViewToWorld, vPR.x*lgtDat.vLaxisX + vPR.y*lgtDat.vLaxisY + vPR.z*lgtDat.vLaxisZ );
#endif
			}
			else
				sampleDir = worldNormalRefl;

			Unity_GlossyEnvironmentData g;
			g.perceptualRoughness = SmoothnessToPerceptualRoughness(data.smoothness);
			g.reflUVW		= sampleDir;

			half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBEARRAY(_reflCubeTextures), lgtDat.iSliceIndex, float4(lgtDat.fLightIntensity, lgtDat.fDecodeExp, 0.0, 0.0), g);
			

			UnityIndirect ind;
			ind.diffuse = 0;
			ind.specular = env0 * data.occlusion;

			half3 rgb = UNITY_BRDF_PBS(0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind).rgb;

			// Calculate falloff value, so reflections on the edges of the Volume would gradually blend to previous reflection.
			// Also this ensures that pixels not located in the reflection Volume AABB won't
			// accidentally pick up reflections from this Volume.
			//half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
			half3 distance = distanceFromAABB(posInReflVolumeSpace, -lgtDat.vBoxInnerDist, lgtDat.vBoxInnerDist);
			half falloff = saturate(1.0 - length(distance)/blendDistance);

			ints = lerp(ints, rgb, falloff);
					
			// next probe
			++l; uIndex = l<uNrLights ? FetchIndex(offs, l) : 0;
			uLgtType = l<uNrLights ? g_vLightData[uIndex].uLightType : 0;
		}

#if !defined(XBONE) && !defined(PLAYSTATION4)
		if(uLgtType!=BOX_LIGHT) ++l;
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


half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBEARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
#if UNITY_GLOSS_MATCHES_MARMOSET_TOOLBAG2 && (SHADER_TARGET >= 30)
	// TODO: remove pow, store cubemap mips differently
	half perceptualRoughness = pow(glossIn.perceptualRoughness, 3.0/4.0);
#else
	half perceptualRoughness = glossIn.perceptualRoughness;			// MM: switched to this
#endif
	//perceptualRoughness = sqrt(sqrt(2/(64.0+2)));		// spec power to the square root of real roughness

#if 0
	float m = perceptualRoughness*perceptualRoughness;				// m is the real roughness parameter
	const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
	float n =  (2.0/max(fEps, m*m))-2.0;		// remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

	n /= 4;									    // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

	perceptualRoughness = pow( 2/(n+2), 0.25);			// remap back to square root of real roughness
#else
	// MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
	perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif



	half mip = perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
	half4 rgbm = UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex, float4(glossIn.reflUVW.xyz, sliceIndex), mip);

	//return rgbm.xyz;
	return DecodeHDR_NoLinearSupportInSM2 (rgbm, hdr);
}



ENDCG 
}

}
Fallback Off
}
