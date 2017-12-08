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
            CGPROGRAM
            #pragma target 3.0

            // needs more than 8 texcoords
            #pragma exclude_renderers gles psp2
            #pragma vertex SplatmapVert
            #pragma fragment SpatmapFragment
            #include "LightweightLighting.cginc"

            #pragma multi_compile _ _MAIN_LIGHT_COOKIE
            #pragma multi_compile _MAIN_DIRECTIONAL_LIGHT _MAIN_SPOT_LIGHT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ _HARD_SHADOWS _SOFT_SHADOWS _HARD_SHADOWS_CASCADES _SOFT_SHADOWS_CASCADES
            #pragma multi_compile _ _VERTEX_LIGHTS

            #pragma multi_compile __ _TERRAIN_NORMAL_MAP
            #pragma multi_compile_fog

            #define TERRAIN_STANDARD_SHADER
            #define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard

            half _Metallic0;
            half _Metallic1;
            half _Metallic2;
            half _Metallic3;

            half _Smoothness0;
            half _Smoothness1;
            half _Smoothness2;
            half _Smoothness3;

            sampler2D _Control;
            float4 _Control_ST;
            sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
            half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;

#ifdef _TERRAIN_NORMAL_MAP
            sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
#endif

            struct VertexOutput
            {
                float4 uvSplat01                : TEXCOORD0; // xy: splat0, zw: splat1
                float4 uvSplat23                : TEXCOORD1; // xy: splat2, zw: splat3
                float4 uvControlAndLM           : TEXCOORD2; // xy: control, zw: lightmap

#if _TERRAIN_NORMAL_MAP
                half3 tangent                   : TEXCOORD3;
                half3 binormal                  : TEXCOORD4;
                half3 normal                    : TEXCOORD5;
#else
                half3 normal                    : TEXCOORD3;
#endif
                half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light
                float3 positionWS               : TEXCOORD7;
                float4 clipPos                  : SV_POSITION;
            };

            void SplatmapMix(VertexOutput IN, half4 defaultAlpha, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
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
                fixed4 nrm = 0.0f;
                nrm += splat_control.r * tex2D(_Normal0, IN.uvSplat01.xy);
                nrm += splat_control.g * tex2D(_Normal1, IN.uvSplat01.zw);
                nrm += splat_control.b * tex2D(_Normal2, IN.uvSplat23.xy);
                nrm += splat_control.a * tex2D(_Normal3, IN.uvSplat23.zw);
                mixedNormal = UnpackNormal(nrm);
#else
                mixedNormal = fixed3(0, 0, 1);
#endif
            }

            VertexOutput SplatmapVert(appdata_full v)
            {
                VertexOutput o;
                UNITY_INITIALIZE_OUTPUT(VertexOutput, o);

                float4 clipPos = UnityObjectToClipPos(v.vertex);
                float3 positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;

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
                o.normal = UnityObjectToWorldNormal(v.normal);
#endif
                o.fogFactorAndVertexLight.x = ComputeFogFactor(clipPos.z);
                o.fogFactorAndVertexLight.yzw = VertexLighting(positionWS, o.normal);
                o.positionWS = positionWS;
                o.clipPos = clipPos;
                return o;
            }

            half4 SpatmapFragment(VertexOutput IN) : SV_TARGET
            {
                half4 splat_control;
                half weight;
                fixed4 mixedDiffuse;
                half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
                half3 normalTangent;
                SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, normalTangent);

                half3 albedo = mixedDiffuse.rgb;
                half smoothness = mixedDiffuse.a;
                half metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
                half3 specular = half3(0, 0, 0);
                half alpha = weight;

#ifdef _TERRAIN_NORMAL_MAP
                half3 normalWS = TangentToWorldNormal(normalTangent, IN.tangent, IN.binormal, IN.normal);
#else
                half3 normalWS = normalize(IN.normal);
#endif

                half3 indirectDiffuse = half3(0, 0, 0);
#if LIGHTMAP_ON
                float2 lightmapUV = IN.uvControlAndLM.zw;
                indirectDiffuse = SampleLightmap(lightmapUV, normalWS);
#endif

                half3 viewDirectionWS = SafeNormalize(_WorldSpaceCameraPos - IN.positionWS);
                half fogFactor = IN.fogFactorAndVertexLight.x;
                half4 color = LightweightFragmentPBR(IN.positionWS, normalWS, viewDirectionWS, indirectDiffuse,
                    IN.fogFactorAndVertexLight.yzw, albedo, metallic, specular, smoothness, /* occlusion */ 1.0, /* emission */ half3(0, 0, 0), alpha);

                ApplyFog(color.rgb, fogFactor);
                return OUTPUT_COLOR(color);
            }
            ENDCG
        }

        Pass
        {
            Tags{"Lightmode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(pos);
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDCG
        }
    }

    Fallback "Hidden/InternalErrorShader"
}
