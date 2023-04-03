Shader "Hidden/Universal Render Pipeline/MaterialError"
{
    SubShader
    {
        Pass
        {
            // Hybrid Renderer compatible error shader, which is used by Hybrid Renderer
            // instead of the incompatible built-in error shader.

            // TODO: Ideally this would be combined with FallbackError.shader, but it seems
            // problematic because FallbackError needs to support SM2.0 and seems to use
            // built-in shader headers, whereas Hybrid support needs SM4.5 and SRP shader headers.
            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            //--------------------------------------
            // GPU Instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(1,0,1,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
