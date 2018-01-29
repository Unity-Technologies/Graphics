Shader "LightweightPipeline/Standard Terrain"
{
    Properties
    {
        // set by terrain engine
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0

        // used in fallback on old cards & base map
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" }

        Pass
        {
            Tags { "LightMode" = "LightweightForward" }
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 3.0

            #pragma vertex SplatmapVert
            #pragma fragment SpatmapFragment

            // -------------------------------------
            // Lightweight Pipeline keywords
            // We have no good approach exposed to skip shader variants, e.g, ideally we would like to skip _CASCADE for all puctual lights
            // Lightweight combines light classification and shadows keywords to reduce shader variants.
            // Lightweight shader library declares defines based on these keywords to avoid having to check them in the shaders
            // Core.hlsl defines _MAIN_LIGHT_DIRECTIONAL and _MAIN_LIGHT_SPOT (point lights can't be main light)
            // Shadow.hlsl defines _SHADOWS_ENABLED, _SHADOWS_SOFT, _SHADOWS_CASCADE, _SHADOWS_PERSPECTIVE
            #pragma multi_compile _ _MAIN_LIGHT_DIRECTIONAL_SHADOW _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE _MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT _MAIN_LIGHT_SPOT_SHADOW _MAIN_LIGHT_SPOT_SHADOW_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_COOKIE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ FOG_LINEAR FOG_EXP2

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED LIGHTMAP_ON
            #pragma multi_compile __ _TERRAIN_NORMAL_MAP

            // LW doesn't support dynamic GI. So we save 30% shader variants if we assume
            // LIGHTMAP_ON when DIRLIGHTMAP_COMBINED is set
            #ifdef DIRLIGHTMAP_COMBINED
            #define LIGHTMAP_ON
            #endif

            #include "LWRP/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(_Terrain)
            half _Metallic0;
            half _Metallic1;
            half _Metallic2;
            half _Metallic3;

            half _Smoothness0;
            half _Smoothness1;
            half _Smoothness2;
            half _Smoothness3;

            float4 _Control_ST;
            half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            CBUFFER_END

            sampler2D _Control;
            sampler2D _Splat0, _Splat1, _Splat2, _Splat3;

#ifdef _TERRAIN_NORMAL_MAP
            sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
#endif

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct VertexOutput
            {
                float4 uvSplat01                : TEXCOORD0; // xy: splat0, zw: splat1
                float4 uvSplat23                : TEXCOORD1; // xy: splat2, zw: splat3
                float4 uvControlAndLM           : TEXCOORD2; // xy: control, zw: lightmap
                half3 normal                    : TEXCOORD3;

#if _TERRAIN_NORMAL_MAP
                half3 tangent                   : TEXCOORD4;
                half3 binormal                  : TEXCOORD5;
#endif
                half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
                float3 positionWS               : TEXCOORD7;

#ifdef _SHADOWS_ENABLED
                float4 shadowCoord               : TEXCOORD8;
#endif

                float4 clipPos                  : SV_POSITION;
            };

            void InitializeInputData(VertexOutput IN, half3 normalTS, out InputData input)
            {
                input = (InputData)0;
                input.positionWS = IN.positionWS;

#ifdef _TERRAIN_NORMAL_MAP
                input.normalWS = TangentToWorldNormal(normalTS, IN.tangent, IN.binormal, IN.normal);
#else
                input.normalWS = normalize(IN.normal);
#endif

                input.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);

#ifdef _SHADOWS_ENABLED
                input.shadowCoord = IN.shadowCoord;
#endif

                input.fogCoord = IN.fogFactorAndVertexLight.x;

#ifdef LIGHTMAP_ON
                input.bakedGI = SampleLightmap(IN.uvControlAndLM.zw, input.normalWS);
#endif
            }

            void SplatmapMix(VertexOutput IN, half4 defaultAlpha, out half4 splat_control, out half weight, out half4 mixedDiffuse, inout half3 mixedNormal)
            {
                splat_control = tex2D(_Control, IN.uvControlAndLM.xy);
                weight = dot(splat_control, half4(1, 1, 1, 1));

#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
                clip(weight == 0.0f ? -1 : 1);
#endif

                // Normalize weights before lighting and restore weights in final modifier functions so that the overal
                // lighting result can be correctly weighted.
                splat_control /= (weight + 1e-3f);

                mixedDiffuse = 0.0f;
                mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uvSplat01.xy) * half4(1.0, 1.0, 1.0, defaultAlpha.r);
                mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uvSplat01.zw) * half4(1.0, 1.0, 1.0, defaultAlpha.g);
                mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uvSplat23.xy) * half4(1.0, 1.0, 1.0, defaultAlpha.b);
                mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uvSplat23.zw) * half4(1.0, 1.0, 1.0, defaultAlpha.a);

#ifdef _TERRAIN_NORMAL_MAP
                half4 nrm = 0.0f;
                nrm += splat_control.r * tex2D(_Normal0, IN.uvSplat01.xy);
                nrm += splat_control.g * tex2D(_Normal1, IN.uvSplat01.zw);
                nrm += splat_control.b * tex2D(_Normal2, IN.uvSplat23.xy);
                nrm += splat_control.a * tex2D(_Normal3, IN.uvSplat23.zw);
                mixedNormal = UnpackNormal(nrm);
#else
                mixedNormal = half3(0, 0, 1);
#endif
            }

            VertexOutput SplatmapVert(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;

                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                float4 clipPos = TransformWorldToHClip(positionWS);

                o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Splat0);
                o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Splat1);
                o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Splat2);
                o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Splat3);
                o.uvControlAndLM.xy = TRANSFORM_TEX(v.texcoord, _Control);
                o.uvControlAndLM.zw = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;

#ifdef _TERRAIN_NORMAL_MAP
                float4 vertexTangent = float4(cross(v.normal, float3(0, 0, 1)), -1.0);
                OutputTangentToWorld(vertexTangent, v.normal, o.tangent, o.binormal, o.normal);
#else
                o.normal = TransformObjectToWorldNormal(v.normal);
#endif
                o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
                o.fogFactorAndVertexLight.yzw = VertexLighting(positionWS, o.normal);
                o.positionWS = positionWS;
                o.clipPos = clipPos;

#if defined(_SHADOWS_ENABLED) && !defined(_SHADOWS_CASCADE)
                o.shadowCoord = ComputeShadowCoord(o.positionWS.xyz);
#endif

                return o;
            }

            half4 SpatmapFragment(VertexOutput IN) : SV_TARGET
            {
                half4 splat_control;
                half weight;
                half4 mixedDiffuse;
                half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
                half3 normalTS;
                SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, normalTS);

                half3 albedo = mixedDiffuse.rgb;
                half smoothness = mixedDiffuse.a;
                half metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
                half3 specular = half3(0, 0, 0);
                half alpha = weight;

                InputData inputData;
                InitializeInputData(IN, normalTS, inputData);
                half4 color = LightweightFragmentPBR(inputData, albedo, metallic, specular, smoothness, /* occlusion */ 1.0, /* emission */ half3(0, 0, 0), alpha);

                ApplyFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Tags{"Lightmode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "LWRP/ShaderLibrary/Core.hlsl"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(pos.xyz);
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}
