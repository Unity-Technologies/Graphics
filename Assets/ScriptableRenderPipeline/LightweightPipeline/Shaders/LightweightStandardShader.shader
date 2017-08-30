Shader "ScriptableRenderPipeline/LightweightPipeline/Standard"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

        // Blending state
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 300

        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Tags{"LightMode" = "LightweightForward"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _PARALLAXMAP

            #pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT _SINGLE_SPOT_LIGHT _SINGLE_POINT_LIGHT
            #pragma multi_compile _ LIGHTWEIGHT_LINEAR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHT_PROBES_ON
            #pragma multi_compile _ _HARD_SHADOWS _SOFT_SHADOWS _HARD_SHADOWS_CASCADES _SOFT_SHADOWS_CASCADES
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #pragma vertex LightweightVertex
            #pragma fragment LightweightFragment
            #include "UnityCG.cginc"
            #include "UnityStandardInput.cginc"
            #include "LightweightPipelineCore.cginc"
            #include "LightweightPipelineLighting.cginc"
            #include "LightweightPipelineBRDF.cginc"

            LightweightVertexOutput LightweightVertex(LightweightVertexInput v)
            {
                LightweightVertexOutput o = (LightweightVertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
                o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
                o.hpos = UnityObjectToClipPos(v.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.posWS.xyz = worldPos;
                
                half3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.viewDir.xyz = viewDir;
#ifndef _METALLICGLOSSMAP
                o.viewDir.w = saturate(_Glossiness + MetallicSetup_Reflectivity()); // grazing term
#endif

                half3 normal = normalize(UnityObjectToWorldNormal(v.normal));

#if _NORMALMAP
                half sign = v.tangent.w * unity_WorldTransformParams.w;
                half3 tangent = normalize(UnityObjectToWorldDir(v.tangent));
                half3 binormal = cross(normal, tangent) * v.tangent.w;

                // Initialize tangetToWorld in column-major to benefit from better glsl matrix multiplication code
                o.tangentToWorld0 = half3(tangent.x, binormal.x, normal.x);
                o.tangentToWorld1 = half3(tangent.y, binormal.y, normal.y);
                o.tangentToWorld2 = half3(tangent.z, binormal.z, normal.z);
#else
                o.normal = normal;
#endif

                o.posWS.w = Pow4(1 - saturate(dot(normal, viewDir))); // fresnel term

#ifdef _LIGHT_PROBES_ON
                o.fogCoord.yzw += max(half3(0, 0, 0), ShadeSH9(half4(normal, 1)));
#endif

                UNITY_TRANSFER_FOG(o, o.hpos);
                return o;
            }

            half4 LightweightFragment(LightweightVertexOutput i) : SV_Target
            {
                float2 uv = i.uv01.xy;
                float2 lightmapUV = i.uv01.zw;

                half4 albedoTex = tex2D(_MainTex, i.uv01.xy);
                half3 albedo = albedoTex.rgb * _Color.rgb;

#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
                half alpha = _Color.a;
                half glossiness = albedorTex.a;
#else
                half alpha = albedoTex.a * _Color.a;
                half glossiness = _Glossiness;
#endif

#if defined(_ALPHATEST_ON)
                clip(alpha - _Cutoff);
#endif

                half2 metallicGloss = MetallicGloss(uv, glossiness);
                half metallic = metallicGloss.x;
                half smoothness = metallicGloss.y;
                half oneMinusReflectivity;
                half3 specColor;

                half3 diffColor = DiffuseAndSpecularFromMetallic(albedo, metallicGloss.x, specColor, oneMinusReflectivity);
                
                half3 normal;
                NormalMap(i, normal);

                // TODO: shader keyword for occlusion
                // TODO: Reflection Probe blend support.
                // GI
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);
                half occlusion = Occlusion(uv);
                UnityIndirect indirectLight = LightweightGI(lightmapUV, i.fogCoord.yzw, reflectVec, occlusion, 1.0h - smoothness);

                // PBS
#ifdef _METALLICGLOSSMAP
                half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
#else
                half grazingTerm = i.viewDir.w;
#endif

                half perVertexFresnelTerm = i.posWS.w;
                half3 color = LightweightBRDFIndirect(diffColor, specColor, indirectLight, grazingTerm, perVertexFresnelTerm);
                half3 lightDirection;

#ifndef _MULTIPLE_LIGHTS
                LightInput light;
                INITIALIZE_MAIN_LIGHT(light);
                half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);

#ifdef _SHADOWS
                lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

                half NdotL = saturate(dot(normal, lightDirection));
                half RdotL = saturate(dot(reflectVec, lightDirection));
                half3 attenuatedLightColor = light.color * (NdotL * lightAtten);

                color += LightweightBRDFDirect(diffColor, specColor, smoothness, RdotL) * attenuatedLightColor;
#else

#ifdef _SHADOWS
                half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif
                int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
                {
                    LightInput light;
                    int lightIndex = unity_4LightIndices0[lightIter];
                    INITIALIZE_LIGHT(light, lightIndex);
                    half lightAtten = ComputeLightAttenuation(light, normal, i.posWS.xyz, lightDirection);
#ifdef _SHADOWS
                    lightAtten *= max(shadowAttenuation, half(lightIter != _ShadowData.x));
#endif
                    half NdotL = saturate(dot(normal, lightDirection));
                    half RdotL = saturate(dot(reflectVec, lightDirection));
                    half3 attenuatedLightColor = light.color * (NdotL * lightAtten);

                    color += LightweightBRDFDirect(diffColor, specColor, smoothness, RdotL) * attenuatedLightColor;
                }
#endif

                color += Emission(uv);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return OutputColor(color, alpha);
            }

            ENDCG
        }
    }
    FallBack "VertexLit"
    CustomEditor "StandardShaderGUI"
}

