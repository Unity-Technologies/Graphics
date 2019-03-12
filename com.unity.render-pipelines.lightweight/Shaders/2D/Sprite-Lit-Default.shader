Shader "Lightweight Render Pipeline/2D/Sprite-Lit-Default"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            // This was basically a test. We should probably make it slightly differently

            Name "Unlit"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                half4 color				: COLOR;
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex	 : SV_POSITION;
                half4  color	 : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            uniform half4 _MainTex_ST;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half3 color = texColor.rgb * input.color.rgb;
                half alpha = texColor.a * input.color.a;
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "CombinedShapeLight" }
            HLSLPROGRAM
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color		: COLOR;
                half2  uv			: TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS		: SV_POSITION;
                float4  color			: COLOR;
                half2	uv				: TEXCOORD0;
                half2	lightingUV		: TEXCOORD1;
                float4  vertexWorldPos	: TEXCOORD3;
                half2	pixelScreenPos	: TEXCOORD4;
            };

            #pragma prefer_hlslcc gles

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __

            #include "Include/CombinedShapeLightPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}
            HLSLPROGRAM
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color		: COLOR;
                half2  uv			: TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS		: SV_POSITION;
                float4  color			: COLOR;
                half2	uv				: TEXCOORD0;
                float3  normal			: TEXCOORD1;
                float3  tangent			: TEXCOORD2;
                float3  bitangent		: TEXCOORD3;
            };

            #pragma prefer_hlslcc gles
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            #include "Include/NormalsRenderingPass.hlsl"
            ENDHLSL
        }
    }
}
