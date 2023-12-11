Shader "Unlit/SimpleSpeedTreeDots"
{
    Properties
    {
        _col("Color", Color) = (0,1,1,1)
        _WindQuality("Wind Quality", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            float4x4 unity_MatrixVP;
            #define UNITY_MATRIX_VP unity_MatrixVP
            #define UNITY_MATRIX_M unity_ObjectToWorld

            #define UNITY_SETUP_DOTS_SH_COEFFS
            #define UNITY_SETUP_DOTS_RENDER_BOUNDS

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _col;
                float _WindQuality;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _col)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #undef unity_ObjectToWorld
                UNITY_DOTS_INSTANCING_START(BuiltinPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float3x4, unity_ObjectToWorld)
                UNITY_DOTS_INSTANCING_END(BuiltinPropertyMetadata)

                #define _col UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _col)
            #else
                CBUFFER_START(UnityPerDraw)
                    float4x4 unity_ObjectToWorld;
                    float4x4 unity_WorldToObject;
                    float4 unity_LODFade;
                    float4 unity_WorldTransformParams;
                CBUFFER_END
            #endif

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(v.vertex)));
                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 c = _col;
                return c;
            }
            ENDHLSL
        }
    }
}
