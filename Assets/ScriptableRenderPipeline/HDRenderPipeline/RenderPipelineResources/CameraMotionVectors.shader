Shader "Hidden/HDRenderPipeline/CameraMotionVectors"
{
    HLSLINCLUDE

        #pragma target 4.5

        #include "../../ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"
        #include "../ShaderPass/FragInputs.hlsl"
        #include "../ShaderPass/VaryingMesh.hlsl"
        #include "../ShaderPass/VertMesh.hlsl"

        float4 _CameraPosDiff;

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
            PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
            float depth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).x;
            UpdatePositionInput(depth, _InvViewProjMatrix, _ViewProjMatrix, posInput);
            float4 worldPos = float4(posInput.positionWS, 1.0);
            float4 prevPos = worldPos;

        #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
            prevPos -= _CameraPosDiff;
        #endif

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
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
