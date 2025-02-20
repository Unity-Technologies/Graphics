Shader "Hidden/CoreSRP/CoreCopy"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        ZClip Off
        ZTest Off 
        ZWrite Off Cull Off 
        Pass
        {
            Name "Copy"
        
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #pragma multi_compile_fragment _ DISABLE_TEXTURE2D_X_ARRAY
            #pragma vertex Vert
            #pragma fragment CopyFrag
            

            // Declares the framebuffer input as a texture 2d containing half.
            FRAMEBUFFER_INPUT_X_FLOAT(0);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
            float4 CopyFrag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // read previous subpasses directly from the framebuffer.
                half4 color = LOAD_FRAMEBUFFER_INPUT_X(0, input.positionCS.xy);
                
                // Modify the sampled color
                return color;
            }
            ENDHLSL
        }

        Tags { "RenderType" = "Opaque" }
        ZClip Off
        ZTest Off
        ZWrite Off Cull Off
        Pass
        {
            Name "CopyMS"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #pragma multi_compile_fragment _ DISABLE_TEXTURE2D_X_ARRAY
            #pragma vertex Vert
            #pragma fragment CopyFragMS
            #pragma target 4.5
            #pragma require msaatex

            // Declares the framebuffer input as a texture 2d containing half.
            FRAMEBUFFER_INPUT_X_FLOAT_MS(0);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
            float4 CopyFragMS(Varyings input, uint sampleID : SV_SampleIndex) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // read previous subpasses directly from the framebuffer.
                half4 color = LOAD_FRAMEBUFFER_INPUT_X_MS(0, sampleID, input.positionCS.xy);

                // Modify the sampled color
                return color;
            }
            ENDHLSL
        }
    }
}

