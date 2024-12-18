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

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile _ DEBUG_DISPLAY

            #pragma vertex TerrainLitVertex
            #pragma fragment TerrainLitForwardFragment

            #if USE_DYNAMIC_BRANCH_FOG_KEYWORD && SHADER_API_VULKAN && SHADER_API_MOBILE
            #define SKIP_SHADOWS_LIGHT_INDEX_CHECK 1
            #endif

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainDetailLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainDetailLitPasses.hlsl"
            ENDHLSL
        }

        // GBuffer
        Pass
        {
            Name "TerrainDetailVertex - GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX //_ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct Attributes
            {
                float4  PositionOS  : POSITION;
                float2  UV0         : TEXCOORD0;
                float2  UV1         : TEXCOORD1;
                half3   NormalOS    : NORMAL;
                half4   Color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2  UV01            : TEXCOORD0; // UV0
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
                half4   Color           : TEXCOORD2; // Vertex Color
                half4   LightingFog     : TEXCOORD3; // Vetex Lighting, Fog Factor
                float4  ShadowCoords    : TEXCOORD4; // Shadow UVs
                half3   NormalWS        : TEXCOORD5; // World Space Normal
                float3  PositionWS      : TEXCOORD6;
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
                OUTPUT_LIGHTMAP_UV(input.UV1, unity_LightmapST, output.staticLightmapUV);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.PositionOS.xyz);
                output.Color = input.Color;
                output.PositionCS = vertexInput.positionCS;

                // Shadow Coords
                output.ShadowCoords = GetShadowCoord(vertexInput);

                // Vertex Lighting
                output.NormalWS = TransformObjectToWorldNormal(input.NormalOS).xyz;

                OUTPUT_SH4(vertexInput.positionWS, output.NormalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, NOT_USED);

                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
                half3 diffuseColor = LightingLambert(attenuatedLightColor, mainLight.direction, output.NormalWS);
            #ifdef _ADDITIONAL_LIGHTS
                int pixelLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, vertexInput.positionWS);
                    half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                    diffuseColor += LightingLambert(attenuatedLightColor, light.direction, output.NormalWS);
                }
            #endif
                output.LightingFog.xyz = diffuseColor;

                // Fog factor
                output.LightingFog.w = ComputeFogFactor(output.PositionCS.z);

                output.PositionWS = vertexInput.positionWS;

                return output;
            }

            GBufferFragOutput Frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                half3 bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(input.PositionWS),
                    input.NormalWS.xyz,
                    GetWorldSpaceNormalizeViewDir(input.PositionWS),
                    input.PositionCS.xy,
                    NOT_USED,
                    NOT_USED);
#else
                half3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.NormalWS);
#endif

                half3 lighting = input.LightingFog.rgb * MainLightRealtimeShadow(input.ShadowCoords) + bakedGI;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.UV01);
                half4 color = 1.0;
                color.rgb = input.Color.rgb * tex.rgb * lighting;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.alpha = 1.0;
                surfaceData.occlusion = 1.0;

                InputData inputData = (InputData)0;
                inputData.normalWS = input.NormalWS;
                inputData.positionCS = input.PositionCS;

                return PackGBuffersSurfaceData(surfaceData, inputData, color.rgb);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask R

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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl"
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
