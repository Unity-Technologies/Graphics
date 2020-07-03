Shader "Universal Render Pipeline/Custom/Surface"
{
    Properties
    {
    	
        [Header(Surface)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/CustomShading.hlsl"
    
        // -------------------------------------
        // Material variables. They need to be declared in UnityPerMaterial
        // to be able to be cached by SRP Batcher
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        CBUFFER_END
    
        // -------------------------------------
        // Textures are declared in global scope
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
    
        void SurfaceFunction(Varyings IN, out CustomSurfaceData surfaceData)
        {
            surfaceData = (CustomSurfaceData)0;
            float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);
            half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

            // diffuse color is black for metals and baseColor for dieletrics
            surfaceData.diffuse = baseColor.rgb;
            surfaceData.normalWS = normalize(IN.normalWS);
            surfaceData.alpha = 1.0;
        }
    
    ENDHLSL

    Subshader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            

            HLSLPROGRAM
            
            #pragma vertex SurfaceVertex
    		#pragma fragment SurfaceFragment

    		

    		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceFunctions.hlsl"
    		

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            ENDHLSL
        }
    }
    
    
}