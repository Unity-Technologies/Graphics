#ifndef __LIGHTINGTEMPLATE_H__
#define __LIGHTINGTEMPLATE_H__


#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"


uniform uint g_nNumDirLights;

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


//UNITY_DECLARE_TEX2D(_LightTextureB0);
sampler2D _LightTextureB0;
UNITY_DECLARE_TEX2DARRAY(_spotCookieTextures);
UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_pointCookieTextures);

StructuredBuffer<DirectionalLight> g_dirLightData;


#define DECLARE_SHADOWMAP( tex ) Texture2D tex; SamplerComparisonState sampler##tex
#ifdef REVERSE_ZBUF
	#define SAMPLE_SHADOW( tex, coord ) tex.SampleCmpLevelZero( sampler##tex, (coord).xy, (coord).z )
#else
	#define SAMPLE_SHADOW( tex, coord ) tex.SampleCmpLevelZero( sampler##tex, (coord).xy, 1.0-(coord).z )
#endif

DECLARE_SHADOWMAP(g_tShadowBuffer);

float ComputeShadow_PCF_3x3_Gaussian(float3 vPositionWs, float4x4 matWorldToShadow)
{
    float4 vPositionTextureSpace = mul(float4(vPositionWs.xyz, 1.0), matWorldToShadow);
    vPositionTextureSpace.xyz /= vPositionTextureSpace.w;

    float2 shadowMapCenter = vPositionTextureSpace.xy;

    if ((shadowMapCenter.x < 0.0f) || (shadowMapCenter.x > 1.0f) || (shadowMapCenter.y < 0.0f) || (shadowMapCenter.y > 1.0f))
        return 1.0f;

    float objDepth = saturate(257.0 / 256.0 - vPositionTextureSpace.z);

    float4 v20Taps;
    v20Taps.x = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.xy, objDepth)).x; //  1  1
    v20Taps.y = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.zy, objDepth)).x; // -1  1
    v20Taps.z = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.xw, objDepth)).x; //  1 -1
    v20Taps.w = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms1.zw, objDepth)).x; // -1 -1
    float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
    if ((flSum == 0.0) || (flSum == 1.0))
        return flSum;
    flSum *= g_vShadow3x3PCFTerms0.x * 4.0;

    float4 v33Taps;
    v33Taps.x = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms2.xz, objDepth)).x; //  1  0
    v33Taps.y = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms3.xz, objDepth)).x; // -1  0
    v33Taps.z = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms3.zy, objDepth)).x; //  0 -1
    v33Taps.w = SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy + g_vShadow3x3PCFTerms2.zy, objDepth)).x; //  0  1
    flSum += dot(v33Taps.xyzw, g_vShadow3x3PCFTerms0.yyyy);

    flSum += SAMPLE_SHADOW(g_tShadowBuffer, float3(shadowMapCenter.xy, objDepth)).x * g_vShadow3x3PCFTerms0.z;

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


float3 ExecuteLightList(uint start, uint numLights, float3 vP, float3 vPw, float3 Vworld)
{
    UnityIndirect ind;
    UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    ind.diffuse = 0;
    ind.specular = 0;


    float3 ints = 0;

    for (int i = 0; i < g_nNumDirLights; i++)
    {
        DirectionalLight lightData = g_dirLightData[i];
        float atten = 1;

        [branch]
        if (lightData.shadowLightIndex != 0xffffffff)
        {
            float shadowScalar = SampleShadow(DIRECTIONAL_LIGHT, vPw, 0, lightData.shadowLightIndex);
            atten *= shadowScalar;
        }

        UnityLight light;
        light.color.xyz = lightData.color.xyz * atten;
        light.dir.xyz = mul((float3x3) g_mViewToWorld, -lightData.lightAxisZ).xyz;

        ints += EvalMaterial(light, ind);
    }

    uint l=0;
	// don't need the outer loop since the lights are sorted by volume type
    //while(l<numLights)
	if(numLights>0)
    {
        uint uIndex = l<numLights ? FetchIndex(start, l) : 0;
        uint uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;

        // specialized loop for spot lights
        while(l<numLights && uLgtType==SPOT_LIGHT)
        {
            SFiniteLightData lgtDat = g_vLightData[uIndex];
            float3 vLp = lgtDat.lightPos.xyz;

            float3 toLight  = vLp - vP;
            float dist = length(toLight);
            float3 vL = toLight / dist;

            float attLookUp = dist*lgtDat.recipRange; attLookUp *= attLookUp;
            float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

            // spot attenuation
            const float fProjVec = -dot(vL, lgtDat.lightAxisZ.xyz);        // spotDir = lgtDat.lightAxisZ.xyz
            float2 cookCoord = (-lgtDat.cotan)*float2( dot(vL, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz) ) / fProjVec;

            const bool bHasCookie = (lgtDat.flags&IS_CIRCULAR_SPOT_SHAPE)==0;       // all square spots have cookies
            float d0 = 0.65;
            float4 angularAtt = float4(1,1,1,smoothstep(0.0, 1.0-d0, 1.0-length(cookCoord)));
            [branch]if(bHasCookie)
            {
                cookCoord = cookCoord*0.5 + 0.5;
                angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, lgtDat.sliceIndex), 0.0);
            }
            atten *= angularAtt.w*(fProjVec>0.0);                           // finally apply this to the dist att.

            const bool bHasShadow = (lgtDat.flags&HAS_SHADOW)!=0;
            [branch]if(bHasShadow)
            {
                float shadowScalar = SampleShadow(SPOT_LIGHT, vPw, 0, lgtDat.shadowLightIndex);
                atten *= shadowScalar;
            }

            UnityLight light;
            light.color.xyz = lgtDat.color.xyz*atten*angularAtt.xyz;
            light.dir.xyz = mul((float3x3) g_mViewToWorld, vL).xyz;     //unity_CameraToWorld

            ints += EvalMaterial(light, ind);

            ++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
            uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;
        }

        // specialized loop for sphere lights
        while(l<numLights && uLgtType==SPHERE_LIGHT)
        {
            SFiniteLightData lgtDat = g_vLightData[uIndex];
            float3 vLp = lgtDat.lightPos.xyz;

            float3 toLight  = vLp - vP;
            float dist = length(toLight);
            float3 vL = toLight / dist;
            float3 vLw = mul((float3x3) g_mViewToWorld, vL).xyz;        //unity_CameraToWorld

            float attLookUp = dist*lgtDat.recipRange; attLookUp *= attLookUp;
            float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

            float4 cookieColor = float4(1,1,1,1);

            const bool bHasCookie = (lgtDat.flags&HAS_COOKIE_TEXTURE)!=0;
            [branch]if(bHasCookie)
            {
                float3 cookieCoord = -float3(dot(vL, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz), dot(vL, lgtDat.lightAxisZ.xyz));    // negate to make vL a fromLight vector
                cookieColor = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(_pointCookieTextures, float4(cookieCoord, lgtDat.sliceIndex), 0.0);
                atten *= cookieColor.w;
            }

            const bool bHasShadow = (lgtDat.flags&HAS_SHADOW)!=0;
            [branch]if(bHasShadow)
            {
                float shadowScalar = SampleShadow(SPHERE_LIGHT, vPw, vLw, lgtDat.shadowLightIndex);
                atten *= shadowScalar;
            }

            UnityLight light;
            light.color.xyz = lgtDat.color.xyz*atten*cookieColor.xyz;
            light.dir.xyz = vLw;

            ints += EvalMaterial(light, ind);

            ++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
            uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;
        }

        //if(uLgtType!=SPOT_LIGHT && uLgtType!=SPHERE_LIGHT) ++l;
    }

    return ints;
}





#endif
