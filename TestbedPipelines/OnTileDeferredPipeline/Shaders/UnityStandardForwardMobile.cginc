#ifndef UNITY_STANDARD_FORWARD_MOBILE_INCLUDED
#define UNITY_STANDARD_FORWARD_MOBILE_INCLUDED


// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#include "UnityStandardConfig.cginc"
#include "UnityStandardCore.cginc"

#include "OnTileShaderBase.h"
#include "../../fptl/LightDefinitions.cs.hlsl"

// todo: put this is LightDefinitions common file
#define MAX_LIGHTS 100

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

#if defined(SHADER_API_D3D11)
#	include "CoreRP/ShaderLibrary/API/D3D11.hlsl"
#elif defined(SHADER_API_PSSL)
#	include "CoreRP/ShaderLibrary/API/PSSL.hlsl"
#elif defined(SHADER_API_XBOXONE)
#	include "CoreRP/ShaderLibrary/API/D3D11.hlsl"
#	include "CoreRP/ShaderLibrary/API/D3D11_1.hlsl"
#elif defined(SHADER_API_METAL)
#	include "ShaderLibrary/API/Metal.hlsl"
#else
#	error unsupported shader api
#endif
#include "CoreRP/ShaderLibrary/API/Validate.hlsl"
#include "../../Fptl/Shadow.hlsl"

struct VertexOutputForwardNew
{
    float4 pos                          : SV_POSITION;
    float4 tex                          : TEXCOORD0;
    half4 ambientOrLightmapUV           : TEXCOORD1;    // SH or Lightmap UV
    half4 tangentToWorldAndParallax[3]  : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:empty]
    float4 posWorld						: TEXCOORD8;
    float4 posView						: TEXCOORD9;

    LIGHTING_COORDS(5,6)
    UNITY_FOG_COORDS(7)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputForwardNew vertForward(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardNew o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardNew, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.posWorld = posWorld;
    o.posView = mul(unity_WorldToCamera, posWorld);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);

    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndParallax[0].xyz = 0;
        o.tangentToWorldAndParallax[1].xyz = 0;
        o.tangentToWorldAndParallax[2].xyz = normalWorld;
    #endif

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    UNITY_TRANSFER_FOG(o,o.pos);

    return o;
}

#define USE_LEFTHAND_CAMERASPACE (0)
#define DIRECT_LIGHT (0)
#define REFLECTION_LIGHT (1)
#define SPOT_LIGHT (0)
#define SPHERE_LIGHT (1)
#define BOX_LIGHT (2)
#define DIRECTIONAL_LIGHT (3)

float4 gPerLightData[MAX_LIGHTS];
half4 gLightColor[MAX_LIGHTS];
float4 gLightPos[MAX_LIGHTS];
half4 gLightDirection[MAX_LIGHTS];
float4x4 gLightMatrix[MAX_LIGHTS];
float4x4 gWorldToLightMatrix[MAX_LIGHTS];
float4  gLightData;

int g_numLights;
int g_numReflectionProbes;
int _useLegacyCookies;
int _transparencyShadows;

float4x4 g_mViewToWorld;
float4x4 g_mWorldToView;        // used for reflection only
float4x4 g_mScrProjection;
float4x4 g_mInvScrProjection;

sampler2D _LightTextureB0;
UNITY_DECLARE_TEX2DARRAY(_spotCookieTextures);
UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_pointCookieTextures);

static FragmentCommonData gdata;
static float occlusion;

// reflections
UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_reflCubeTextures);
UNITY_DECLARE_TEXCUBE(_reflRootCubeTexture);
uniform float _reflRootHdrDecodeMult;
uniform float _reflRootHdrDecodeExp;

StructuredBuffer<SFiniteLightData> g_vProbeData;

// ---- Utilities ---- //

void GetCountAndStart(out uint start, out uint nrLights, uint model)
{
    start = model==REFLECTION_LIGHT ? g_numLights : 0;  // offset by numLights entries
    nrLights = model==REFLECTION_LIGHT ? g_numReflectionProbes : g_numLights;
}

// ---- Reflections ---- //

half3 Unity_GlossyEnvironment (UNITY_ARGS_ABSTRACT_CUBE_ARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn);

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
    return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

float3 EvalIndirectSpecular(UnityLight light, UnityIndirect ind)
{
    return occlusion * UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}

float3 RenderReflectionList(uint start, uint numReflProbes, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    float3 worldNormalRefl = reflect(-Vworld, vNw);

    float3 vspaceRefl = mul((float3x3) g_mWorldToView, worldNormalRefl).xyz;

    float percRoughness = SmoothnessToPerceptualRoughness(smoothness);

    UnityLight light;
    light.color = 0;
    light.dir = 0;

    float3 ints = 0;

    // root ibl begin
    {
        Unity_GlossyEnvironmentData g;
        g.roughness = percRoughness;
        g.reflUVW = worldNormalRefl;

        half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(_reflRootCubeTexture), float4(_reflRootHdrDecodeMult, _reflRootHdrDecodeExp, 0.0, 0.0), g);
        //half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBEARRAY(_reflCubeTextures), _reflRootSliceIndex, float4(_reflRootHdrDecodeMult, _reflRootHdrDecodeExp, 0.0, 0.0), g);

        UnityIndirect ind;
        ind.diffuse = 0;
        ind.specular = env0;// * data.occlusion;
        ints = EvalIndirectSpecular(light, ind);
    }
    // root ibl end

    for (int uIndex=0; uIndex<gLightData.y; uIndex++)
    {
        SFiniteLightData lgtDat = g_vProbeData[uIndex];
        float3 vLp = lgtDat.lightPos.xyz;
        float3 vecToSurfPos  = vP - vLp;        // vector from reflection volume to surface position in camera space
        float3 posInReflVolumeSpace = float3( dot(vecToSurfPos, lgtDat.lightAxisX), dot(vecToSurfPos, lgtDat.lightAxisY), dot(vecToSurfPos, lgtDat.lightAxisZ) );


        float blendDistance = lgtDat.probeBlendDistance;//unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe

        float3 sampleDir;
        if((lgtDat.flags&IS_BOX_PROJECTED)!=0)
        {
            // For box projection, use expanded bounds as they are rendered; otherwise
            // box projection artifacts when outside of the box.
            //float4 boxMin = unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
            //float4 boxMax = unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
            //sampleDir = BoxProjectedCubemapDirection (worldNormalRefl, worldPos, unity_SpecCube0_ProbePosition, boxMin, boxMax);

            float4 boxOuterDistance = float4( lgtDat.boxInnerDist + float3(blendDistance, blendDistance, blendDistance), 0.0 );
#if 0
            // if rotation is NOT supported
            sampleDir = BoxProjectedCubemapDirection(worldNormalRefl, posInReflVolumeSpace, float4(lgtDat.localCubeCapturePoint, 1.0), -boxOuterDistance, boxOuterDistance);
#else
            float3 volumeSpaceRefl = float3( dot(vspaceRefl, lgtDat.lightAxisX), dot(vspaceRefl, lgtDat.lightAxisY), dot(vspaceRefl, lgtDat.lightAxisZ) );
            float3 vPR = BoxProjectedCubemapDirection(volumeSpaceRefl, posInReflVolumeSpace, float4(lgtDat.localCubeCapturePoint, 1.0), -boxOuterDistance, boxOuterDistance);    // Volume space corrected reflection vector
            sampleDir = mul( (float3x3) g_mViewToWorld, vPR.x*lgtDat.lightAxisX + vPR.y*lgtDat.lightAxisY + vPR.z*lgtDat.lightAxisZ );
#endif
        }
        else
            sampleDir = worldNormalRefl;

        Unity_GlossyEnvironmentData g;
        g.roughness = percRoughness;
        g.reflUVW       = sampleDir;

        half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_ABSTRACT_CUBE_ARRAY(_reflCubeTextures), lgtDat.sliceIndex, float4(lgtDat.lightIntensity, lgtDat.decodeExp, 0.0, 0.0), g);

        UnityIndirect ind;
        ind.diffuse = 0;
        ind.specular = env0;// * data.occlusion;

        //half3 rgb = UNITY_BRDF_PBS(0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, vWSpaceVDir, light, ind).rgb;
        half3 rgb = EvalIndirectSpecular(light, ind);

        // Calculate falloff value, so reflections on the edges of the Volume would gradually blend to previous reflection.
        // Also this ensures that pixels not located in the reflection Volume AABB won't
        // accidentally pick up reflections from this Volume.
        //half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
        half3 distance = distanceFromAABB(posInReflVolumeSpace, -lgtDat.boxInnerDist, lgtDat.boxInnerDist);
        half falloff = saturate(1.0 - length(distance)/blendDistance);

        ints = lerp(ints, rgb, falloff);
    }

    return ints;
}

half3 Unity_GlossyEnvironment (UNITY_ARGS_ABSTRACT_CUBE_ARRAY(tex), int sliceIndex, half4 hdr, Unity_GlossyEnvironmentData glossIn)
{
#if UNITY_GLOSS_MATCHES_MARMOSET_TOOLBAG2 && (SHADER_TARGET >= 30)
    // TODO: remove pow, store cubemap mips differently
    half perceptualRoughness = pow(glossIn.roughness, 3.0/4.0);
#else
    half perceptualRoughness = glossIn.roughness;           // MM: switched to this
#endif
    //perceptualRoughness = sqrt(sqrt(2/(64.0+2)));     // spec power to the square root of real roughness

#if 0
    float m = perceptualRoughness*perceptualRoughness;              // m is the real roughness parameter
    const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
    float n =  (2.0/max(fEps, m*m))-2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

    n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

    perceptualRoughness = pow( 2/(n+2), 0.25);          // remap back to square root of real roughness
#else
    // MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
    perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif



    half mip = perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
    half4 rgbm = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(tex, float4(glossIn.reflUVW.xyz, sliceIndex), mip);

    return DecodeHDR(rgbm, hdr);
}

float3 ExecuteReflectionList(out uint numReflectionProbesProcessed, uint2 pixCoord, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    uint start = 0, numReflectionProbes = 0;
    GetCountAndStart(start, numReflectionProbes, REFLECTION_LIGHT);

    numReflectionProbesProcessed = numReflectionProbes;     // mainly for debugging/heat maps
    return RenderReflectionList(start, numReflectionProbes, vP, vNw, Vworld, smoothness);
}

// ---- Lights ---- //

float3 EvalMaterial(UnityLight light, UnityIndirect ind)
{
    return UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}

float3 RenderLightList(uint start, uint numLights, float3 vPw, float3 Vworld)
{
    UnityIndirect ind;
    UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    ind.diffuse = 0;
    ind.specular = 0;

    ShadowContext shadowContext = InitShadowContext();

    float3 ints = 0;

    for (int lightIndex = 0; lightIndex < gLightData.x; ++lightIndex)
    {
  		if (gPerLightData[lightIndex].x == DIRECTIONAL_LIGHT)
  		{
  			float atten = 1;

	  		int shadowIdx = gPerLightData[lightIndex].y;
			[branch]
			if (shadowIdx >= 0 && _transparencyShadows)
			{
				float shadow = GetDirectionalShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, 0.0.xxx);
				atten *= shadow;
			}

			float4 cookieColor = float4(1,1,1,1);
			float4 uvCookie = mul (gLightMatrix[lightIndex], float4(vPw,1));
            float2 cookCoord = uvCookie.xy / uvCookie.w;
			const bool bHasCookie = gPerLightData[lightIndex].z >= 0;
            [branch]if(bHasCookie)
            {
       			cookieColor = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, gPerLightData[lightIndex].z), 0.0);
       			atten *= cookieColor.w;
            }
            [branch]if(_useLegacyCookies)
            {
            	cookieColor.xyz = 1;
            }

	        UnityLight light;
	       	light.color.xyz = gLightColor[lightIndex].xyz*atten*cookieColor.xyz;

	        light.dir.xyz = -gLightDirection[lightIndex].xyz;

	        ints += EvalMaterial(light, ind);
  		}
  		else if (gPerLightData[lightIndex].x == SPHERE_LIGHT)
  		{
  			float3 vLp = gLightPos[lightIndex].xyz;

            float3 toLight  = vLp - vPw;
            float dist = length(toLight);
            float3 vLw = toLight / dist;

            float att = dot(toLight, toLight) * gLightPos[lightIndex].w;
			float atten = tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;

            float4 cookieColor = float4(1,1,1,1);
            const bool bHasCookie = gPerLightData[lightIndex].z >= 0;
            [branch]if(bHasCookie)
            {
            	float4 uvCookie = mul (gLightMatrix[lightIndex], float4(vLw,1));
            	float3 cookieCoord = -uvCookie.xyz / uvCookie.w;
                cookieColor = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(_pointCookieTextures, float4(cookieCoord, gPerLightData[lightIndex].z), 0.0);
                atten *= cookieColor.w;
            }
            [branch]if(_useLegacyCookies)
            {
            	cookieColor.xyz = 1;
            }

			int shadowIdx = gPerLightData[lightIndex].y;
			[branch]
			if (shadowIdx >= 0 && _transparencyShadows)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, float4(vLw, dist));
				atten *= shadow;
			}

            UnityLight light;
            light.color.xyz = gLightColor[lightIndex].xyz*atten*cookieColor.xyz;
            light.dir.xyz = vLw;

            ints += EvalMaterial(light, ind);
  		}
  		else if (gPerLightData[lightIndex].x == SPOT_LIGHT)
  		{
            float3 vLp = gLightPos[lightIndex].xyz;

            float3 toLight  = vLp - vPw;
            float dist = length(toLight);
            float3 vLw = toLight / dist;

            // distance atten
			float att = dot(toLight, toLight) * gLightPos[lightIndex].w;
			float atten = tex2Dlod (_LightTextureB0, float4(att.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

            float4 uvCookie = mul (gLightMatrix[lightIndex], float4(vPw,1));
            float2 cookCoord = uvCookie.xy / uvCookie.w;

            float d0 = 0.65;
            float4 angularAtt = float4(1,1,1,smoothstep(0.0, 1.0-d0, 1.0-length(2*cookCoord-1)));
            const bool bHasCookie = gPerLightData[lightIndex].z >= 0;
            [branch]if(bHasCookie)
            {
               angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, gPerLightData[lightIndex].z), 0.0);
            }
            [branch]if(_useLegacyCookies)
            {
            	angularAtt.xyz = 1;
            }
            atten *= angularAtt.w*(-uvCookie.w>0.0);                           // finally apply this to the dist att.

			int shadowIdx = gPerLightData[lightIndex].y;
			[branch]
			if (shadowIdx >= 0 && _transparencyShadows)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, float4(vLw, dist));
				atten *= shadow;
			}

            UnityLight light;
            light.color.xyz = gLightColor[lightIndex].xyz*atten*angularAtt.xyz;

            light.dir.xyz = vLw.xyz;     //unity_CameraToWorld

            ints += EvalMaterial(light, ind);
  		}
    }

    return ints;
}

float3 ExecuteLightList(out uint numLightsProcessed, uint2 pixCoord, float3 vPw, float3 Vworld)
{
    uint start = 0, numLights = 0;
    GetCountAndStart(start, numLights, DIRECT_LIGHT);

    numLightsProcessed = numLights;     // mainly for debugging/heat maps
    return RenderLightList(start, numLights, vPw, Vworld);
}

// fragment shader main
half4 singlePassForward(VertexOutputForwardNew i)
{
	// matching script side where camera space is right handed.
    float3 vP = i.posView;
    float3 vPw = i.posWorld;
    float3 Vworld = normalize(_WorldSpaceCameraPos.xyz - vPw);

#ifdef _PARALLAXMAP
    half3 tangent = i.tangentToWorldAndParallax[0].xyz;
    half3 bitangent = i.tangentToWorldAndParallax[1].xyz;
    half3 normal = i.tangentToWorldAndParallax[2].xyz;
    float3 vDirForParallax = float3( dot(tangent, Vworld), dot(bitangent, Vworld), dot(normal, Vworld));
#else
    float3 vDirForParallax = Vworld;
#endif
    gdata = FragmentSetup(i.tex, -Vworld, vDirForParallax, i.tangentToWorldAndParallax, vPw);       // eyeVec = -Vworld

    uint2 pixCoord = ((uint2) i.pos.xy);

    float atten = 1.0;
    occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI (gdata, occlusion, i.ambientOrLightmapUV, atten, DummyLight(), false);

    uint numLightsProcessed = 0, numReflectionsProcessed = 0;
    float3 res = 0;

    // direct light contributions
    res += ExecuteLightList(numLightsProcessed, pixCoord, vPw, Vworld);

    // specular GI
    res += ExecuteReflectionList(numReflectionsProcessed, pixCoord, vP, gdata.normalWorld, Vworld, gdata.smoothness);

    // diffuse GI
    res += UNITY_BRDF_PBS (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, gi.light, gi.indirect).xyz;
    res += UNITY_BRDF_GI (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, occlusion, gi);

	return OutputForward (float4(res,1.0), gdata.alpha);

}

#endif
