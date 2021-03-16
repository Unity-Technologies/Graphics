Shader "Hidden/HDRP/ProbeVolumeGizmo"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/EditorShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"

        uniform int _ShadingMode;
        uniform float _ExposureCompensation;
        uniform float _ProbeSize;
        uniform float4 _Color;

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float3 normal : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _R)
            UNITY_DEFINE_INSTANCED_PROP(float4, _G)
            UNITY_DEFINE_INSTANCED_PROP(float4, _B)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Validity)
        UNITY_INSTANCING_BUFFER_END(Props)

        v2f vert(appdata v)
        {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            o.vertex = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize, 1.0)));
            o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));

            return o;
        }

        float3 evalSH(float3 normal, float4 SHAr, float4 SHAg, float4 SHAb)
        {
            float4 normalPadded = float4(normal, 1);

            float3 x;

            SHAr.xyz = DecodeSH(SHAr.w, SHAr.xyz);
            SHAg.xyz = DecodeSH(SHAg.w, SHAg.xyz);
            SHAb.xyz = DecodeSH(SHAb.w, SHAb.xyz);

            // Linear (L1) + constant (L0) polynomial terms
            x.r = dot(SHAr, normalPadded);
            x.g = dot(SHAg, normalPadded);
            x.b = dot(SHAb, normalPadded);

            return x;
        }

        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i);
            if (_ShadingMode == 1)
            {
                float4 r = UNITY_ACCESS_INSTANCED_PROP(Props, _R);
                float4 g = UNITY_ACCESS_INSTANCED_PROP(Props, _G);
                float4 b = UNITY_ACCESS_INSTANCED_PROP(Props, _B);

                return float4(evalSH(normalize(i.normal), r, g, b) * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
            }
            else if (_ShadingMode == 2)
            {
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
            }
            return _Color;
        }
        ENDHLSL

        Pass
        {
            Name "DepthForwardOnly"
            Tags{ "LightMode" = "DepthForwardOnly" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

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
