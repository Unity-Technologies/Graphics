// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
Shader "Hidden/TerrainEngine/Details/Vertexlit" 
{
    Properties 
    {
        _MainTex ("Main Texture", 2D) = "white" {  }
    }    
    SubShader 
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
        LOD 100
        
        ZWrite On
        
        // Lightmapped
        Pass
        {
            Name "TerrainDetailVertex"
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            
            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS_PIXEL
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
    
            float4 _MainTex_ST;
    
            struct Attributes
            {
                float4  PositionOS  : POSITION;
                float2  UV0         : TEXCOORD0;
                float2  UV1         : TEXCOORD1;
                float3  NormalOS    : NORMAL;
                half4   Color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
    
            struct Varyings
            {
                float2  UV01            : TEXCOORD0; // UV0
                float2  LightmapUV      : TEXCOORD1; // Lightmap UVs
                half4   Color           : TEXCOORD2; // Vertex Color
                half4   LightingFog     : TEXCOORD3; // Vetex Lighting, Fog Factor
                float4  ShadowCoords    : TEXCOORD4; // Shadow UVs
                float4  PositionCS      : SV_POSITION; // Clip Position
                UNITY_VERTEX_OUTPUT_STEREO
            };
    
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Vertex attributes
                output.UV01 = TRANSFORM_TEX(input.UV0, _MainTex);
                output.LightmapUV = input.UV1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                VertexPosition vertexPosition = GetVertexPosition(input.PositionOS.xyz);
                output.Color = input.Color;
                output.PositionCS = vertexPosition.hclipSpace;
                
                // Shadow Coords
                output.ShadowCoords = GetShadowCoord(vertexPosition);

                // Vertex Lighting
                half3 NormalWS = input.NormalOS;
                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
                half3 diffuseColor = LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);
            #ifdef _ADDITIONAL_LIGHTS
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, vertexPosition.worldSpace);
                    half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                    diffuseColor += LightingLambert(attenuatedLightColor, light.direction, NormalWS);
                }
            #endif
                output.LightingFog.xyz = diffuseColor;
                
                // Fog factor
                output.LightingFog.w = ComputeFogFactor(output.PositionCS.z);
                
                return output;
            }
    
            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
    
            half4 Frag(Varyings input) : SV_Target
            {
                half3 bakedGI = SampleLightmap(input.LightmapUV, half3(0.0, 1.0, 0.0));
                
                half3 lighting = input.LightingFog.rgb * MainLightRealtimeShadow(input.ShadowCoords) + bakedGI;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
                half4 color = 1.0;
                color.rgb = input.Color.rgb * tex.rgb * lighting;
    
                ApplyFog(color.rgb, input.LightingFog.w);
    
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Depth"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/InputSurfaceUnlit.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/LightweightPassDepthOnly.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "Meta"
            Tags{ "LightMode" = "Meta" }
            
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMetaSimple
            
            #pragma shader_feature _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/InputSurfaceSimple.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/LightweightPassMetaSimple.hlsl"

            ENDHLSL
        }
    }
    
    //Fallback "VertexLit"
}
