Shader "Hidden/HDRP/ProbeVolumeOffsetDebug"
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

            float3 offset = UNITY_ACCESS_INSTANCED_PROP(Props, _Offset).xyz;
            float offsetLenSqr = dot(offset, offset);
            if(offsetLenSqr <= 1e-6f)
            {
                DoCull(o);
            }
            else if(!ShouldCull(o))
            {
                float4 wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.x * _OffsetSize, v.vertex.y * _OffsetSize, v.vertex.z, 1.f));
                o.vertex = mul(UNITY_MATRIX_VP, wsPos);
                o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
            }

            return o;
        }

        float4 frag(v2f i) : SV_Target
        {
            return float4(0, 0, 1, 1);
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
