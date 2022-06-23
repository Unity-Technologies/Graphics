Shader "Hidden/HDRP/MaterialError"
{
    SubShader
    {
            Pass
            {
                Name "DebugMaterialID"

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

                Texture2D<float> _MaterialDepth;
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
                    float materialDepth = (LOAD_TEXTURE2D(_MaterialDepth, uv).r);
                    uint materialID = asuint(materialDepth);
                    materialID *= 3.141592657;
                    materialID &= 0x3FFF;
                    float4 color;
                    color.r = (materialID & 0x2491) / 256.0f;
                    color.g = (materialID & (0x824)) / 256.0f;
                    color.b = (materialID & (0x4A2)) / 256.0f;
                    color.a = 1.0f;
                    return color;
                }
                ENDHLSL
            }

            Pass
            {
                Name "DebugMaterialRange"

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

                StructuredBuffer<MaterialRange> _MaterialRangeBuffer;
                uniform uint2 _Viewport;
                uniform uint2 _TileSize;

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
                    uint2 xy = uv * _Viewport;
                    xy = uv / 64;
                    uint id = (xy.y) * _TileSize.x + xy.x;
                    MaterialRange range = _MaterialRangeBuffer[id];
                    uint materialMin = (range.min/* * 3.1416*/);
                    uint materialMax = (range.max/* * 3.1416*/);
                    float4 color;
                    color.r = (materialMin & 0x3FFF) / 256.0f;
                    color.g = (materialMax & 0x3FFF) / 256.0f;
                    color.b = 0.0f;
                    color.a = 1.0f;
                    return color;
                }
                ENDHLSL
            }
    }
    Fallback Off
}
