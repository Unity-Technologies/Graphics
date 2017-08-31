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
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag

#pragma multi_compile USE_FPTL_LIGHTLIST    USE_CLUSTERED_LIGHTLIST
#pragma multi_compile __ ENABLE_DEBUG

#include "UnityLightingCommon.cginc"

float3 EvalMaterial(UnityLight light, UnityIndirect ind);

// uses the optimized single layered light list for opaques only

#ifdef USE_FPTL_LIGHTLIST
    #define OPAQUES_ONLY
#endif

#include "TiledLightingTemplate.hlsl"


UNITY_DECLARE_TEX2D_FLOAT(_CameraDepthTexture);
Texture2D _CameraGBufferTexture0;
Texture2D _CameraGBufferTexture1;
Texture2D _CameraGBufferTexture2;
Texture2D _CameraGBufferTexture3;


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

struct StandardData
{
    float3 specularColor;
    float3 diffuseColor;
    float3 normalWorld;
    float smoothness;
    float3 emission;
};

struct LocalDataBRDF
{
    StandardData gbuf;

    // extras
    float oneMinusReflectivity;
    float3 Vworld;
};

static LocalDataBRDF g_localParams;

StandardData UnityStandardDataFromGbufferAux(float4 gbuffer0, float4 gbuffer1, float4 gbuffer2, float4 gbuffer3)
{
    StandardData data;

    data.normalWorld = normalize(2*gbuffer2.xyz-1);
    data.smoothness = gbuffer1.a;
    data.diffuseColor = gbuffer0.xyz; data.specularColor = gbuffer1.xyz;
    float ao = gbuffer0.a;
    data.emission = gbuffer3.xyz;

    return data;
}


float3 EvalMaterial(UnityLight light, UnityIndirect ind)
{
    StandardData data = g_localParams.gbuf;
    return UNITY_BRDF_PBS(data.diffuseColor, data.specularColor, g_localParams.oneMinusReflectivity, data.smoothness, data.normalWorld, g_localParams.Vworld, light, ind);
}


half4 frag (v2f i) : SV_Target
{
    uint2 pixCoord = ((uint2) i.vertex.xy);

    float zbufDpth = FetchDepth(_CameraDepthTexture, pixCoord.xy).x;
    float linDepth = GetLinearDepth(zbufDpth);

    float3 vP = GetViewPosFromLinDepth(i.vertex.xy, linDepth);
    float3 vPw = mul(g_mViewToWorld, float4(vP, 1)).xyz;
    float3 Vworld = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);     //unity_CameraToWorld

    float4 gbuffer0 = _CameraGBufferTexture0.Load( uint3(pixCoord.xy, 0) );
    float4 gbuffer1 = _CameraGBufferTexture1.Load( uint3(pixCoord.xy, 0) );
    float4 gbuffer2 = _CameraGBufferTexture2.Load( uint3(pixCoord.xy, 0) );
    float4 gbuffer3 = _CameraGBufferTexture3.Load( uint3(pixCoord.xy, 0) );

    StandardData data = UnityStandardDataFromGbufferAux(gbuffer0, gbuffer1, gbuffer2, gbuffer3);

    g_localParams.gbuf = data;
    g_localParams.oneMinusReflectivity = 1.0 - SpecularStrength(data.specularColor.rgb);
    g_localParams.Vworld = Vworld;

    uint numLightsProcessed = 0;
    float3 c = data.emission + ExecuteLightList(numLightsProcessed, pixCoord, vP, vPw, Vworld);

#if ENABLE_DEBUG
    c = OverlayHeatMap(pixCoord & 15, numLightsProcessed, c);
#endif
    return float4(c,1.0);
}

ENDCG
}

}
Fallback Off
}
