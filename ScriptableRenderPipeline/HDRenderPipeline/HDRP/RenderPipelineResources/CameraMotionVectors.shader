Shader "Hidden/HDRenderPipeline/CameraMotionVectors"
{
    HLSLINCLUDE

        #pragma target 4.5

        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"
        #include "../ShaderPass/FragInputs.hlsl"
        #include "../ShaderPass/VaryingMesh.hlsl"
        #include "../ShaderPass/VertMesh.hlsl"

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            return output;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            float depth = LOAD_TEXTURE2D(_MainDepthTexture, input.positionCS.xy).x;
        
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP);

            float4 worldPos = float4(posInput.positionWS, 1.0);
            float4 prevPos = worldPos;

            float4 prevClipPos = mul(_PrevViewProjMatrix, prevPos);
            float4 curClipPos = mul(_NonJitteredViewProjMatrix, worldPos);
            float2 prevHPos = prevClipPos.xy / prevClipPos.w;
            float2 curHPos = curClipPos.xy / curClipPos.w;

            float2 previousPositionCS = (prevHPos + 1.0) / 2.0;
            float2 positionCS = (curHPos + 1.0) / 2.0;

        #if UNITY_UV_STARTS_AT_TOP
            previousPositionCS.y = 1.0 - previousPositionCS.y;
            positionCS.y = 1.0 - positionCS.y;
        #endif

            return float4(positionCS - previousPositionCS, 0.0, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        
        Pass
        {
            // We will perform camera motion velocity only where there is no object velocity
            Stencil
            {
                ReadMask 128
                Ref  128 // StencilBitMask.ObjectVelocity
                Comp NotEqual
                Pass Keep
            }

            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
