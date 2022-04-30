Shader "Hidden/HDRP/DebugVisibility"
{
    SubShader
    {
        Pass
        {
            Name "DebugClusterID"

            ZTest Off
            ZWrite Off

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Camera/GPUDrivenCommon.hlsl"

            struct appdata_t {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Texture2D<uint4> _VisibilityBuffer;
            float _MaxRange;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                uint2 uv = i.positionCS.xy;
                uint clusterID = (LOAD_TEXTURE2D(_VisibilityBuffer, uv).r);
                float id = clusterID / _MaxRange;
                float4 color;
                color.rgb = id;
                //color.r = ((clusterID >> 16) & 0xFF) / 256.0f;
                //color.r = clusterID;
                //color.g = ((clusterID >> 8) & 0xFF) / 256.0f;
                //color.b = (clusterID & 0xFF) / 256.0f;
                color.a = 1.0f;
                return color;
                //return float4(1,0,1,1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DebugTriangleID"

            ZTest Off
            ZWrite Off

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Camera/GPUDrivenCommon.hlsl"

            struct appdata_t {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Texture2D<uint4> _VisibilityBuffer;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                uint2 uv = i.positionCS.xy;
                uint triangleID = (LOAD_TEXTURE2D(_VisibilityBuffer, uv).g);
                float id = triangleID / 128.0f;
                float4 color;
                color.rgb = id;
                color.a = 1.0f;
                return color;
            }
            ENDHLSL
        }

        
    }
    Fallback Off
}
