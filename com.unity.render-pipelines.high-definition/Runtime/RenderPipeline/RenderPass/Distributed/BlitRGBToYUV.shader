Shader "Hidden/HDRP/Distributed/BlitRGBToYUV"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
        #pragma multi_compile _ BLIT_SINGLE_SLICE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_BlitTexture);
        SamplerState sampler_PointClamp;
        SamplerState sampler_LinearClamp;
        SamplerState sampler_PointRepeat;
        SamplerState sampler_LinearRepeat;
        uniform int _BlitTexArraySlice;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float3 RGBToYUV(float3 rgb)
        {
            float y =  0.299f   * rgb.r + 0.587f   * rgb.g + 0.114f   * rgb.b;
        	float u = -0.14713f * rgb.r - 0.28886f * rgb.g + 0.436f   * rgb.b + 0.5f;
        	float v =  0.615f   * rgb.r - 0.51499f * rgb.g - 0.10001f * rgb.b + 0.5f;
			return float3(y, u, v);
        }

        float4 Frag(Varyings input, SamplerState s)
        {
            float4 rgb;
        #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTexture, s, input.texcoord.xy, _BlitTexArraySlice, 0);
        #endif

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            rgb = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, s, input.texcoord.xy, 0);

            return float4(RGBToYUV(rgb.rgb), 1);
        }

        float4 FragY(Varyings input) : SV_Target
        {
            return Frag(input, sampler_LinearClamp).rrrr;
        }
    
        float4 FragU(Varyings input) : SV_Target
        {
            return Frag(input, sampler_LinearClamp).gggg;
        }
    
        float4 FragV(Varyings input) : SV_Target
        {
            return Frag(input, sampler_LinearClamp).bbbb;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragY
            ENDHLSL
        }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragU
            ENDHLSL
        }

        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragV
            ENDHLSL
        }

    }

    Fallback Off
}