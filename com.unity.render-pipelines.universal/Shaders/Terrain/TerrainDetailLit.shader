Shader "Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {  }
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Unlit" "IgnoreProjector" = "True"}
        LOD 100

        ZWrite On

        // Lightmapped
        Pass
        {
            Name "TerrainDetailVertex"
            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
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
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4  ShadowCoords    : TEXCOORD4; // Shadow UVs
#endif
                float4  PositionCS      : SV_POSITION; // Clip Position

                UNITY_VERTEX_INPUT_INSTANCE_ID
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
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
                output.Color = input.Color;
                output.PositionCS = vertexInput.positionCS;

                // Shadow Coords
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    output.ShadowCoords = GetShadowCoord(vertexInput);
                #endif

                // Vertex Lighting
                half3 NormalWS = input.NormalOS;
                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
                half3 diffuseColor = LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    int pixelLightCount = GetAdditionalLightsCount();
                    for (int i = 0; i < pixelLightCount; ++i)
                    {
                        Light light = GetAdditionalLight(i, vertexInput.positionWS);
                        half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, NormalWS);
                    }
                #endif

                output.LightingFog.xyz = diffuseColor;

                // Fog factor
                output.LightingFog.w = ComputeFogFactor(output.PositionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 bakedGI = SampleLightmap(input.LightmapUV, half3(0.0, 1.0, 0.0));

                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    half3 lighting = input.LightingFog.rgb * MainLightRealtimeShadow(input.ShadowCoords) + bakedGI;
                #else
                    half3 lighting = input.LightingFog.rgb + bakedGI;
                #endif

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
                half4 color = 1.0;
                color.rgb = input.Color.rgb * tex.rgb * lighting;

                color.rgb = MixFog(color.rgb, input.LightingFog.w);

                return color;
            }
            ENDHLSL
        }

        // GBuffer
        Pass
        {
            Name "TerrainDetailVertex - GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            HLSLPROGRAM
            #pragma exclude_renderers gles
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX //_ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
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

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Vertex attributes
                output.UV01 = TRANSFORM_TEX(input.UV0, _MainTex);
                output.LightmapUV = input.UV1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
                output.Color = input.Color;
                output.PositionCS = vertexInput.positionCS;

                // Shadow Coords
                output.ShadowCoords = GetShadowCoord(vertexInput);

                // Vertex Lighting
                half3 NormalWS = input.NormalOS;
                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
                half3 diffuseColor = LightingLambert(attenuatedLightColor, mainLight.direction, NormalWS);
            #ifdef _ADDITIONAL_LIGHTS
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, vertexInput.positionWS);
                    half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                    diffuseColor += LightingLambert(attenuatedLightColor, light.direction, NormalWS);
                }
            #endif
                output.LightingFog.xyz = diffuseColor;

                // Fog factor
                output.LightingFog.w = ComputeFogFactor(output.PositionCS.z);

                return output;
            }

            FragmentOutput Frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 bakedGI = SampleLightmap(input.LightmapUV, half3(0.0, 1.0, 0.0));

                half3 lighting = input.LightingFog.rgb * MainLightRealtimeShadow(input.ShadowCoords) + bakedGI;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
                half4 color = 1.0;
                color.rgb = input.Color.rgb * tex.rgb * lighting;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.alpha = 1.0;

                InputData inputData = (InputData)0;
                inputData.normalWS = half3(0, 1, 0); // need some default to avoid division by 0.

                return SurfaceDataToGbuffer(surfaceData, inputData, color.rgb, kLightingInvalid);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaSimple

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitMetaPass.hlsl"

            ENDHLSL
        }
    }

    //Fallback "VertexLit"
}
