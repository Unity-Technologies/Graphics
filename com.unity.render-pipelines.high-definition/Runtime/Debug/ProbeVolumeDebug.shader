Shader "Hidden/HDRP/ProbeVolumeDebug"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        #include "ProbeVolumeDebug.hlsl"

        v2f vert(appdata v)
        {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            if(!ShouldCull(o))
            {
                float4 wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, wsPos);
                o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
            }

            return o;
        }

        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i);

            if (_ShadingMode >= DEBUGPROBESHADINGMODE_SH && _ShadingMode <= DEBUGPROBESHADINGMODE_SHL0L1)
            {
                return float4(CalculateDiffuseLighting(i) * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_INVALIDATED_BY_TOUCHUP_VOLUMES)
            {
                float4 defaultCol = float4(CalculateDiffuseLighting(i) * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
                float touchupAction = UNITY_ACCESS_INSTANCED_PROP(Props, _TouchupedByVolume);
                if (touchupAction > 0 && touchupAction < 1)
                {
                    return float4(1, 0, 0, 1);
                }
                return defaultCol;
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY)
            {
                float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
                return lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), validity);
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY_OVER_DILATION_THRESHOLD)
            {
                float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
                if (validity > _ValidityThreshold)
                {
                    return float4(1, 0, 0, 1);
                }
                else
                {
                    return float4(0, 1, 0, 1);
                }
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_SIZE)
            {
                float4 col = lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), UNITY_ACCESS_INSTANCED_PROP(Props, _RelativeSize));
                return col;
            }

            return _Color;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
