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
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
            #pragma vertex Vert
            #pragma fragment CopyFrag
            

            // Declares the framebuffer input as a texture 2d containing half.
            FRAMEBUFFER_INPUT_FLOAT(0);

            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
            float4 CopyFrag(Varyings input) : SV_Target0
            {        
                // read the current pixel from the framebuffer
                float2 uv = input.texcoord.xy;
                // read previous subpasses directly from the framebuffer.
                half4 color = LOAD_FRAMEBUFFER_INPUT(0, input.positionCS.xy);
                
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
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment CopyFragMS
            #pragma target 4.5
            #pragma require msaatex

            // Declares the framebuffer input as a texture 2d containing half.
            FRAMEBUFFER_INPUT_FLOAT_MS(0);

            // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
            float4 CopyFragMS(Varyings input, uint sampleID : SV_SampleIndex) : SV_Target0
            {
                // read the current pixel from the framebuffer
                float2 uv = input.texcoord.xy;
                // read previous subpasses directly from the framebuffer.
                half4 color = LOAD_FRAMEBUFFER_INPUT_MS(0, sampleID, input.positionCS.xy);

                // Modify the sampled color
                return color;
            }
            ENDHLSL
        }
    }
}

