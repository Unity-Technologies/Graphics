// Based on AMD FSR3 auto reactive mask generation.
// https://github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK/blob/v2.3.0/Kits/FidelityFX/upscalers/fsr3/internal/shaders/ffx_fsr3upscaler_autogen_reactive_pass.hlsl
Shader "Hidden/Universal Render Pipeline/UpscalerReactiveMask"
{
    Properties
    {
        // Defaults to the value required by STP.
        [HideInInspector] _StencilRef("_StencilRef", Int) = 1
        [HideInInspector] _StencilMask("_StencilMask", Int) = 1
    }

    HLSLINCLUDE
    #pragma target 4.5
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

    #define AUTOREACTIVEFLAGS_APPLY_TONEMAP           1
    #define AUTOREACTIVEFLAGS_APPLY_INVERSETONEMAP    2
    #define AUTOREACTIVEFLAGS_APPLY_THRESHOLD         4
    #define AUTOREACTIVEFLAGS_USE_COMPONENTS_MAX      8

    CBUFFER_START(UpscalerReactiveMaskParams)
        float _ReactiveScale;
        float _ReactiveThreshold;
        float _ReactiveBinaryValue;
        uint  _ReactiveFlags;
    CBUFFER_END

    TEXTURE2D(_ColorPreAlpha);
    TEXTURE2D(_ColorPostAlpha);
    SAMPLER(s_point_clamp_sampler);

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    float3 Tonemap(float3 rgb)
    {
        return rgb / (max(max(0.f, rgb.r), max(rgb.g, rgb.b)) + 1.0).xxx;
    }

    float3 InverseTonemap(float3 rgb)
    {
        return rgb / max(FLT_MIN, 1.0 - max(rgb.r, max(rgb.g, rgb.b))).xxx;
    }

    float ComputeReactivity(float2 uv)
    {
        float3 colorPreAlpha = SAMPLE_TEXTURE2D_LOD(_ColorPreAlpha, s_point_clamp_sampler, uv, 0).rgb;
        float3 colorPostAlpha = SAMPLE_TEXTURE2D_LOD(_ColorPostAlpha, s_point_clamp_sampler, uv, 0).rgb;

        if (_ReactiveFlags & AUTOREACTIVEFLAGS_APPLY_TONEMAP)
        {
            colorPreAlpha = Tonemap(colorPreAlpha);
            colorPostAlpha = Tonemap(colorPostAlpha);
        }

        if (_ReactiveFlags & AUTOREACTIVEFLAGS_APPLY_INVERSETONEMAP)
        {
            colorPreAlpha = InverseTonemap(colorPreAlpha);
            colorPostAlpha = InverseTonemap(colorPostAlpha);
        }

        float reactive = 0.0;
        float3 delta = abs(colorPostAlpha - colorPreAlpha);

        reactive = (_ReactiveFlags & AUTOREACTIVEFLAGS_USE_COMPONENTS_MAX) ? max(delta.x, max(delta.y, delta.z)) : length(delta);
        reactive *= _ReactiveScale;

        if ((_ReactiveFlags & AUTOREACTIVEFLAGS_APPLY_THRESHOLD))
        {
            reactive = (reactive < _ReactiveThreshold) ? 0 : _ReactiveBinaryValue;
        }

        return reactive;
    }

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ColorReactiveMask"

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ColorReactiveMaskPS

            void ColorReactiveMaskPS(Varyings input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float reactive = ComputeReactivity(input.texcoord.xy);

                outColor = float4(reactive, 1, 1, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "StencilReactiveMask"
            
            Stencil
            {
                ReadMask [_StencilMask]
                Ref      [_StencilRef]
                Comp Always
                Pass Replace
            }
            
            ZWrite Off ZTest Always Blend Off Cull Off
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment StencilReactiveMaskPS
            
            void StencilReactiveMaskPS(Varyings input, out float4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float reactive = ComputeReactivity(input.texcoord.xy);

                if (reactive == 0)
                    discard;

                outColor = float4(reactive, 1, 1, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
