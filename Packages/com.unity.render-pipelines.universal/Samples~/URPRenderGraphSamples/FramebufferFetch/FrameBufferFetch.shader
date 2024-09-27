Shader "FrameBufferFetch"
{
   SubShader
   {
       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "FrameBufferFetch"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag

           // Declares the framebuffer input as a texture 2d containing half.
           FRAMEBUFFER_INPUT_HALF(0);

           // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
           float4 Frag(Varyings input) : SV_Target0
           {
               // this is needed so we account XR platform differences in how they handle texture arrays
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // read the current pixel from the framebuffer
               float2 uv = input.texcoord.xy;
               // read previous subpasses directly from the framebuffer.
               half4 color = LOAD_FRAMEBUFFER_INPUT(0, input.positionCS.xy);
               
               // Modify the sampled color
               return half4(0,0,1,1) * color;
           }

           ENDHLSL
       }

       Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
       ZWrite Off Cull Off
       Pass
       {
           Name "FrameBufferFetchMS"

           HLSLPROGRAM
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

           #pragma vertex Vert
           #pragma fragment Frag
           #pragma target 4.5
           #pragma require msaatex

           // Declares the framebuffer input as a texture 2d containing half.
           FRAMEBUFFER_INPUT_HALF_MS(0);

           // Out frag function takes as input a struct that contains the screen space coordinate we are going to use to sample our texture. It also writes to SV_Target0, this has to match the index set in the UseTextureFragment(sourceTexture, 0, …) we defined in our render pass script.   
           float4 Frag(Varyings input, uint sampleID : SV_SampleIndex) : SV_Target0
           {
               // this is needed so we account XR platform differences in how they handle texture arrays
               UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

               // read the current pixel from the framebuffer
               float2 uv = input.texcoord.xy;
               // read previous subpasses directly from the framebuffer.
               half4 color = LOAD_FRAMEBUFFER_INPUT_MS(0, sampleID, input.positionCS.xy);
               
               // Modify the sampled color
               return half4(0,0,1,1) * color;
           }

           ENDHLSL
       }
   }
}