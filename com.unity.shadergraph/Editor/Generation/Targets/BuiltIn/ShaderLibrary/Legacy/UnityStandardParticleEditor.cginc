#ifndef UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED
#define UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardParticleInstancing.cginc"

#ifdef _ALPHATEST_ON
half        _Cutoff;
#endif
sampler2D   _MainTex;
float4      _MainTex_ST;

float _ObjectId;
float _PassValue;
float4 _SelectionID;
uniform float _SelectionAlphaCutoff;

struct VertexInput
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    fixed4 color    : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        float4 texcoords : TEXCOORD0;
        float texcoordBlend : TEXCOORD1;
    #else
        float2 texcoords : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 texcoord : TEXCOORD0;
    #ifdef _FLIPBOOK_BLENDING
        float3 texcoord2AndBlend : TEXCOORD1;
    #endif
    fixed4 color : TEXCOORD2;
};

void vertEditorPass(VertexInput v, out VertexOutput o, out float4 opos : SV_POSITION)
{
    UNITY_SETUP_INSTANCE_ID(v);

    opos = UnityObjectToClipPos(v.vertex);

    #ifdef _FLIPBOOK_BLENDING
        #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
            vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
        #else
            o.texcoord = v.texcoords.xy;
            o.texcoord2AndBlend.xy = v.texcoords.zw;
            o.texcoord2AndBlend.z = v.texcoordBlend;
        #endif
    #else
        #ifdef UNITY_PARTICLE_INSTANCING_ENABLED
            vertInstancingUVs(v.texcoords.xy, o.texcoord);
            o.texcoord = TRANSFORM_TEX(o.texcoord, _MainTex);
        #else
            o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
        #endif
    #endif
    o.color = v.color;
}

void fragSceneClip(VertexOutput i)
{
    half alpha = tex2D(_MainTex, i.texcoord).a;
#ifdef _FLIPBOOK_BLENDING
    half alpha2 = tex2D(_MainTex, i.texcoord2AndBlend.xy);
    alpha = lerp(alpha, alpha2, i.texcoord2AndBlend.z);
#endif
    alpha *= i.color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif
}

half4 fragSceneHighlightPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return float4(_ObjectId, _PassValue, 1, 1);
}

half4 fragScenePickingPass(VertexOutput i) : SV_Target
{
    fragSceneClip(i);
    return _SelectionID;
}

#endif // UNITY_STANDARD_PARTICLE_EDITOR_INCLUDED
