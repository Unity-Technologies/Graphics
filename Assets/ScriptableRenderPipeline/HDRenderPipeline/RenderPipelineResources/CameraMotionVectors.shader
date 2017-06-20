Shader "Hidden/HDRenderPipeline/CameraMotionVectors"
{
    HLSLINCLUDE

        #pragma target 4.5

        #include "../../ShaderLibrary/Common.hlsl"
        #include "../ShaderVariables.hlsl"
        #include "../ShaderPass/FragInputs.hlsl"
        #include "../ShaderPass/VaryingMesh.hlsl"
        #include "../ShaderPass/VertMesh.hlsl"

        PackedVaryingsType Vert(AttributesMesh inputMesh)
        {
            VaryingsType varyingsType;
            varyingsType.vmesh = VertMesh(inputMesh);
            return PackVaryingsType(varyingsType);
        }

        float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
        {
            PositionInputs posInput = GetPositionInput(packedInput.vmesh.positionCS.xy, _ScreenSize.zw);
            float depth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).x;
            float3 vPos = ComputeViewSpacePosition(posInput.positionSS, depth, _InvProjMatrix);
            float4 worldPos = mul(unity_CameraToWorld, float4(vPos, 1.0));

            float4 prevClipPos = mul(_PrevViewProjMatrix, worldPos);
            float4 curClipPos = mul(_ViewProjMatrix, worldPos);
            float2 prevHPos = prevClipPos.xy / prevClipPos.w;
            float2 curHPos = curClipPos.xy / curClipPos.w;

            float2 previousPositionCS = (prevHPos + 1.0) / 2.0;
            float2 positionCS = (curHPos + 1.0) / 2.0;

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
