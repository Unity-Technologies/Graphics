Shader "MobileRenderloop/ClassicDeferredReflections" {
Properties {
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}
SubShader {

// Calculates reflection contribution from a single probe (rendered as cubes) or default reflection (rendered as full screen quad)
Pass {
	Name "DEFERRED_REFLECTIONS"

    ZWrite Off
    ZTest LEqual
    Blend One Zero
CGPROGRAM
#pragma target 4.5
#pragma vertex filip_vert_deferred
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityPBSLighting.cginc"

#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
#endif

unity_v2f_deferred filip_vert_deferred (float4 vertex : POSITION, float3 normal : NORMAL)
{
    bool lightAsQuad = _LightAsQuad!=0.0;

    unity_v2f_deferred o;

    // scaling quasd by two becuase built-in unity quad ranges from -0.5 to 0.5
    o.pos = lightAsQuad ? float4(2.0*vertex.xy, 0.5, 1.0) : UnityObjectToClipPos(vertex);
    o.uv = ComputeScreenPos(o.pos);

    // normal contains a ray pointing from the camera to one of near plane's
    // corners in camera space when we are drawing a full screen quad.
    // Otherwise, when rendering 3D shapes, use the ray calculated here.
    if (lightAsQuad){
    	float2 rayXY = mul(unity_CameraInvProjection, float4(o.pos.x, -o.pos.y, -1, 1)).xy;
        o.ray = float3(rayXY, 1.0);
    }
    else
    {
    	o.ray = UnityObjectToViewPos(vertex) * float3(-1,-1,1);
    }
    return o;
}

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
    return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

#ifdef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
void frag (unity_v2f_deferred i,
	in half4 outGBuffer0 : SV_Target0,
	in half4 outGBuffer1 : SV_Target1,
	in half4 outGBuffer2 : SV_Target2,
	out half4 outEmission : SV_Target3, 
	in float outLinearDepth : SV_Target4)
#else
half4 frag (unity_v2f_deferred i) : SV_TARGET
#endif
{
	//return half4(1.0, 0.0, 0.0, 1.0);

    // Stripped from UnityDeferredCalculateLightParams, refactor into function ?
    i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
    float2 uv = i.uv.xy / i.uv.w;

    // read depth and reconstruct world position
	#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	#else
		float depth = outLinearDepth;
	#endif

    depth = Linear01Depth (depth);
    float4 viewPos = float4(i.ray * depth,1);
    float3 worldPos = mul (unity_CameraToWorld, viewPos).xyz;

#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
	// unpack Gbuffer
	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
#endif
    UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

    float3 eyeVec = normalize(worldPos - _WorldSpaceCameraPos);
    half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor);

// ---

    half3 worldNormalRefl = reflect(eyeVec, data.normalWorld);

    // Unused member don't need to be initialized
    UnityGIInput d;
    d.worldPos = worldPos;
    d.worldViewDir = -eyeVec;
    d.probeHDR[0] = unity_SpecCube0_HDR;

    float blendDistance = unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    d.probePosition[0]  = unity_SpecCube0_ProbePosition;
    d.boxMin[0].xyz     = unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
    d.boxMin[0].w       = 1;  // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call
    d.boxMax[0].xyz     = unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
    #endif

    Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(data.smoothness, d.worldViewDir, data.normalWorld, data.specularColor);

    half3 env0 = UnityGI_IndirectSpecular(d, data.occlusion, g);

    UnityLight light;
    light.color = half3(0, 0, 0);
    light.dir = half3(0, 1, 0);

// --- 
    UnityIndirect ind;
    ind.diffuse = 0;
    ind.specular = env0;

    half3 rgb = UNITY_BRDF_PBS (0, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind).rgb;
// ---

    // Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
    // Also this ensures that pixels not located in the reflection probe AABB won't
    // accidentally pick up reflections from this probe.
    half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
    half falloff = saturate(1.0 - length(distance)/blendDistance);

    return half4(rgb, falloff);
}

ENDCG
}

// Adds reflection buffer to the lighting buffer
Pass
{
	Name "DEFERRED_APPLY_REFLECTIONS"

    ZWrite Off
    ZTest Always
    Blend One One

    CGPROGRAM
        #pragma target 4.5
        #pragma vertex refl_apply_vert_deferred
        #pragma fragment frag
        #pragma multi_compile ___ UNITY_HDR_ON

        #include "UnityCG.cginc"

        sampler2D _CameraReflectionsTexture;

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 pos : SV_POSITION;
        };

        v2f refl_apply_vert_deferred (float4 vertex : POSITION, float3 normal : NORMAL)
		{
		    // scaling quasd by two becuase built-in unity quad ranges from -0.5 to 0.5
		  	v2f o;
		    o.pos = float4(2.0*vertex.xy, 0.5, 1.0);
		    o.uv = ComputeScreenPos(o.pos);

		    return o;
		}

        half4 frag (v2f i) : SV_Target
        {
            half4 c = tex2D (_CameraReflectionsTexture, i.uv);
            #ifdef UNITY_HDR_ON
            return float4(c.rgb, 0.0f);
            #else
            return float4(exp2(-c.rgb), 0.0f);
            #endif

        }
    ENDCG
}

}
Fallback Off
}
