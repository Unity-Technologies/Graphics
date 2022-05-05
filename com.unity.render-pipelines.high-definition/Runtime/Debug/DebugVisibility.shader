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
                uint clusterID = GetClusterID(LOAD_TEXTURE2D(_VisibilityBuffer, uv).r);
                if (clusterID == ~0u)
                    return float4(0, 0, 0, 1);

                clusterID = _ClusterIDBuffer[clusterID].clusterID;
                clusterID *= 3.141592657f;
                float4 color;
                color.r = ((clusterID) & 0x924924) / 256.0f;
                color.g = ((clusterID) & 0x492492) / 256.0f;
                color.b = (clusterID & 0x249248) / 256.0f;
                color.rgb = 0.299f * color.r + 0.578f * color.g + 0.114f * color.b;
                //color.rgb = 0.33f * color.r + 0.34f * color.g + 0.33f * color.b;
                color.a = 1.0f;
                return color;
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

            Pass
            {
                Name "DebugInstanceID"

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
                    uint clusterID = GetClusterID(LOAD_TEXTURE2D(_VisibilityBuffer, uv).r);
                    if (clusterID == ~0u)
                        return float4(0, 0, 0, 1);

                    uint instanceID = _ClusterIDBuffer[clusterID].instanceID;
                    //clusterID *= 3.141592657f;
                    float4 color;
                    color.r = ((instanceID) & 0x924924) / 2560.0f;
                    color.g = ((instanceID) & 0x492492) / 2560.0f;
                    color.b = (instanceID & 0x249248) / 2560.0f;
                    color.rgb = 0.299f * color.r + 0.578f * color.g + 0.114f * color.b;
                    //color.rgb = 0.33f * color.r + 0.34f * color.g + 0.33f * color.b;
                    color.a = 1.0f;
                    return color;
                }
                ENDHLSL
            }

    }
    Fallback Off
}
