Shader "Hidden/Lightweight Render Pipeline/Terrain/Lit_BasemapGen"
{
    Properties
    {
        [HideInInspector] _Control("AlphaMap", 2D) = "" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}        
        [HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }
    
    Subshader
    {
        HLSLINCLUDE
        // Required to compile gles 2.0 with standard srp library
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #pragma target 3.0
        
        #define _METALLICSPECGLOSSMAP 1
        #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1


        #pragma shader_feature _TERRAIN_BLEND_HEIGHT
        //#pragma shader_feature _NORMALMAP
        #pragma shader_feature _MASKMAP
        // Sample normal in pixel shader when doing instancing
        //#pragma shader_feature _TERRAIN_INSTANCED_PERPIXEL_NORMAL

        #include "TerrainLitInput.hlsl"
        #include "TerrainLitPasses.hlsl"
        
        struct Attributes
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings
        {
            float4 clipPos : SV_POSITION;
            float4 uvMainAndLM : TEXCOORD0;
            float4 uvSplat01 : TEXCOORD1;
            float4 uvSplat23 : TEXCOORD2;
        };
       
        ENDHLSL
        
        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]     
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            
            Varyings Vert(Attributes IN)
            {
                Varyings output;
                
                output.clipPos = TransformWorldToHClip(IN.vertex);
                
                // NOTE : This is basically coming from the vertex shader in TerrainLitPasses
                // There are other plenty of other values that the original version computes, but for this
                // pass, we are only interested in a few, so I'm just skipping the rest.
                output.uvMainAndLM.xy = IN.texcoord;
                output.uvSplat01.xy = TRANSFORM_TEX(IN.texcoord, _Splat0);
                output.uvSplat01.zw = TRANSFORM_TEX(IN.texcoord, _Splat1);
                output.uvSplat23.xy = TRANSFORM_TEX(IN.texcoord, _Splat2);
                output.uvSplat23.zw = TRANSFORM_TEX(IN.texcoord, _Splat3);

                return output;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
                half4 splatControl;
                half weight;
                half4 mixedDiffuse = 0.0h;
    
                half4 masks[4];
                splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, IN.uvMainAndLM.xy);
                
            #ifdef _MASKMAP
                masks[0] = SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, IN.uvSplat01.xy);
                masks[1] = SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, IN.uvSplat01.zw);
                masks[2] = SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, IN.uvSplat23.xy);
                masks[3] = SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, IN.uvSplat23.zw);
                
            #ifdef _TERRAIN_BLEND_HEIGHT
                HeightBasedSplatModify(splatControl, masks);
            #endif                
                
            #else
                masks[0] = half4(1.0h, 1.0h, 0.0h, mixedDiffuse.a);
                masks[1] = half4(1.0h, 1.0h, 0.0h, mixedDiffuse.a);
                masks[2] = half4(1.0h, 1.0h, 0.0h, mixedDiffuse.a);
                masks[3] = half4(1.0h, 1.0h, 0.0h, mixedDiffuse.a);
            #endif
            
                SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, normalTS);
            
                half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
                defaultSmoothness *= half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
                defaultSmoothness *= half4(_MaskMapRemapScale0.a, _MaskMapRemapScale1.a, _MaskMapRemapScale2.a, _MaskMapRemapScale3.a);
                defaultSmoothness += half4(_MaskMapRemapOffset0.a, _MaskMapRemapOffset1.a, _MaskMapRemapOffset2.a, _MaskMapRemapOffset3.a);
                half smoothness = dot(splatControl, defaultSmoothness);

                return half4(mixedDiffuse.rgb, smoothness);
            }

            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "R8"
                "Size" = "1/4"
                "EmptyColor" = "FF000000"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]     

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            
            Varyings Vert(Attributes IN)
            {
                Varyings output;
                
                output.clipPos = TransformWorldToHClip(IN.vertex);
                
                // This is just like the other in that it is from TerrainLitPasses
                output.uvMainAndLM.xy = IN.texcoord;
                output.uvSplat01.xy = TRANSFORM_TEX(IN.texcoord, _Splat0);
                output.uvSplat01.zw = TRANSFORM_TEX(IN.texcoord, _Splat1);
                output.uvSplat23.xy = TRANSFORM_TEX(IN.texcoord, _Splat2);
                output.uvSplat23.zw = TRANSFORM_TEX(IN.texcoord, _Splat3);
                
                return output;
            }
            
            half4 Frag(Varyings IN) : SV_Target
            {
                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
                half4 splatControl;
                half weight;
                half4 mixedDiffuse;
    
                half4 masks[4];
                splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, IN.uvMainAndLM.xy);
                
            #ifdef _MASKMAP
                masks[0] = SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, IN.uvSplat01.xy);
                masks[1] = SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, IN.uvSplat01.zw);
                masks[2] = SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, IN.uvSplat23.xy);
                masks[3] = SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, IN.uvSplat23.zw);
                
            #ifdef _TERRAIN_BLEND_HEIGHT
                HeightBasedSplatModify(splatControl, masks);
            #endif  
            #else
                masks[0] = half4(1.0h, 1.0h, 0.0h, 0.0h);
                masks[1] = half4(1.0h, 1.0h, 0.0h, 0.0h);
                masks[2] = half4(1.0h, 1.0h, 0.0h, 0.0h);
                masks[3] = half4(1.0h, 1.0h, 0.0h, 0.0h);
            #endif
                SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, normalTS);
            
                half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
                defaultMetallic *= half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
                defaultMetallic *= half4(_MaskMapRemapScale0.r, _MaskMapRemapScale1.r, _MaskMapRemapScale3.r, _MaskMapRemapScale3.r);
                defaultMetallic += half4(_MaskMapRemapOffset0.r, _MaskMapRemapOffset1.r, _MaskMapRemapOffset2.r, _MaskMapRemapOffset3.r);
                half metallic = dot(splatControl, defaultMetallic);
                
                return metallic;
            }
            
            ENDHLSL
        }
    }
}