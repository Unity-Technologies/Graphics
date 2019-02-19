Shader "Lightweight Render Pipeline/Examples/UnlitOutline"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _OutlineColor("OutlineColor", Color) = (1, 1, 1, 1)
        _OutlinePushScale("OutlinePushScale", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM

            #pragma vertex vertex
            #pragma fragment fragment

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _OutlineColor;
            half  _OutlinePushScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 vertex       : SV_POSITION;
            };

            Varyings vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            half4 fragment(Varyings input) : SV_Target
            {

                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 color = texColor.rgb * _BaseColor.rgb;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Tags{"LightMode" = "Outline"}
            Cull Front

            HLSLPROGRAM

            #pragma vertex vertex
            #pragma fragment fragment

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _OutlineColor;
            half  _OutlinePushScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float3 normalOS         : NORMAL;
            };

            float4 vertex(Attributes input) : SV_POSITION
            {
                float3 normal = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normal * _OutlinePushScale.xxx;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
                return vertexInput.positionCS;
            }

            half4 fragment() : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
