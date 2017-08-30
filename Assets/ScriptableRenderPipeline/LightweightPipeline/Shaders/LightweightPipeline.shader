// Shader targeted for low end devices. Single Pass Forward Rendering. Shader Model 2
Shader "ScriptableRenderPipeline/LightweightPipeline/NonPBR"
{
    // Keep properties of StandardSpecular shader for upgrade reasons.
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Shininess("Shininess", Range(0.01, 1.0)) = 1.0
        _GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0

        _Glossiness("Glossiness", Range(0.0, 1.0)) = 0.5
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Cube ("Reflection Cubemap", CUBE) = "" {}
        _ReflectionSource("Reflection Source", Float) = 0

        [HideInInspector] _SpecSource("Specular Color Source", Float) = 0.0
        _SpecColor("Specular", Color) = (1.0, 1.0, 1.0)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [HideInInspector] _GlossinessSource("Glossiness Source", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
        _ParallaxMap("Height Map", 2D) = "black" {}

        _EmissionColor("Emission Color", Color) = (0,0,0)
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
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" }
        LOD 300

        Pass
        {
            Tags { "LightMode" = "LightweightForward" }

            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON
            #pragma shader_feature _ _SPECGLOSSMAP _SPECGLOSSMAP_BASE_ALPHA _SPECULAR_COLOR
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ _REFLECTION_CUBEMAP _REFLECTION_PROBE

            #pragma multi_compile _ LIGHTWEIGHT_LINEAR
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT _SINGLE_SPOT_LIGHT _SINGLE_POINT_LIGHT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _LIGHT_PROBES_ON
            #pragma multi_compile _ _HARD_SHADOWS _SOFT_SHADOWS _HARD_SHADOWS_CASCADES _SOFT_SHADOWS_CASCADES
            #pragma multi_compile _ _VERTEX_LIGHTS
            #pragma multi_compile _ _ATTENUATION_TEXTURE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "UnityStandardInput.cginc"
            #include "LightweightPipelineCore.cginc"
            #include "LightweightPipelineLighting.cginc"

            LightweightVertexOutput vert(LightweightVertexInput v)
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

                o.viewDir.xyz = normalize(_WorldSpaceCameraPos - worldPos);
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

                // TODO: change to only support point lights per vertex. This will greatly simplify shader ALU
#if defined(_VERTEX_LIGHTS) && defined(_MULTIPLE_LIGHTS)
                half3 diffuse = half3(1.0, 1.0, 1.0);
                // pixel lights shaded = min(pixelLights, perObjectLights)
                // vertex lights shaded = min(vertexLights, perObjectLights) - pixel lights shaded
                // Therefore vertexStartIndex = pixelLightCount;  vertexEndIndex = min(vertexLights, perObjectLights)
                int vertexLightStart = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                int vertexLightEnd = min(globalLightCount.y, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = vertexLightStart; lightIter < vertexLightEnd; ++lightIter)
                {
                    int lightIndex = unity_4LightIndices0[lightIter];
                    LightInput lightInput;
                    INITIALIZE_LIGHT(lightInput, lightIndex);

                    half3 lightDirection;
                    half atten = ComputeLightAttenuationVertex(lightInput, normal, worldPos, lightDirection);
                    o.fogCoord.yzw += LightingLambert(diffuse, lightDirection, normal, atten);
                }
#endif

#ifdef _LIGHT_PROBES_ON
                o.fogCoord.yzw += max(half3(0, 0, 0), ShadeSH9(half4(normal, 1)));
#endif

                UNITY_TRANSFER_FOG(o, o.hpos);
                return o;
            }

            half4 frag(LightweightVertexOutput i) : SV_Target
            {
                half4 diffuseAlpha = tex2D(_MainTex, i.uv01.xy);
                half3 diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(diffuseAlpha.rgb) * _Color.rgb;
                half alpha = diffuseAlpha.a * _Color.a;

                // Keep for compatibility reasons. Shader Inpector throws a warning when using cutoff
                // due overdraw performance impact.
#ifdef _ALPHATEST_ON
                clip(alpha - _Cutoff);
#endif

                half3 normal;
                NormalMap(i, normal);

                half4 specularGloss;
                SpecularGloss(i.uv01.xy, alpha, specularGloss);

                half3 viewDir = i.viewDir.xyz;
                float3 worldPos = i.posWS.xyz;

                half3 lightDirection;
                
#ifndef _MULTIPLE_LIGHTS
                LightInput lightInput;
                INITIALIZE_MAIN_LIGHT(lightInput);
                half lightAtten = ComputeLightAttenuation(lightInput, normal, worldPos, lightDirection);
#ifdef _SHADOWS
                lightAtten *= ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
                half3 color = LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightInput.color;
#else
                half3 color = LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightInput.color;
#endif
    
#else
                half3 color = half3(0, 0, 0);

#ifdef _SHADOWS
                half shadowAttenuation = ComputeShadowAttenuation(i, _ShadowLightDirection.xyz);
#endif
                int pixelLightCount = min(globalLightCount.x, unity_LightIndicesOffsetAndCount.y);
                for (int lightIter = 0; lightIter < pixelLightCount; ++lightIter)
                {
                    LightInput lightData;
                    int lightIndex = unity_4LightIndices0[lightIter];
                    INITIALIZE_LIGHT(lightData, lightIndex);
                    half lightAtten = ComputeLightAttenuation(lightData, normal, worldPos, lightDirection);
#ifdef _SHADOWS
                    lightAtten *= max(shadowAttenuation, half(lightIter != _ShadowData.x));
#endif

#ifdef LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
                    color += LightingBlinnPhong(diffuse, specularGloss, lightDirection, normal, viewDir, lightAtten) * lightData.color;
#else
                    color += LightingLambert(diffuse, lightDirection, normal, lightAtten) * lightData.color;
#endif
                }

#endif // _MULTIPLE_LIGHTS

#ifdef _EMISSION
                color += LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, i.uv01.xy).rgb) * _EmissionColor;
#else
                color += _EmissionColor;
#endif

#if defined(LIGHTMAP_ON)
                color += (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv01.zw)) + i.fogCoord.yzw) * diffuse;
#elif defined(_VERTEX_LIGHTS) || defined(_LIGHT_PROBES_ON)
                color += i.fogCoord.yzw * diffuse;
#endif

#if _REFLECTION_CUBEMAP
                // TODO: we can use reflect vec to compute specular instead of half when computing cubemap reflection
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);
                color += texCUBE(_Cube, reflectVec).rgb * specularGloss.rgb;
#elif defined(_REFLECTION_PROBE)
                half3 reflectVec = reflect(-i.viewDir.xyz, normal);
                half4 reflectionProbe = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectVec);
                color += reflectionProbe.rgb * (reflectionProbe.a * unity_SpecCube0_HDR.x) * specularGloss.rgb;
#endif

                UNITY_APPLY_FOG(i.fogCoord, color);

                return OutputColor(color, alpha);
            };
            ENDCG
        }

        Pass
        {
            Tags { "Lightmode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                float4 clipPos = UnityObjectToClipPos(pos);
#if defined(UNITY_REVERSED_Z)
                clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
                clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
                return clipPos;
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDCG
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Tags{ "LightMode" = "Meta" }

            Cull Off

            CGPROGRAM
            #define UNITY_SETUP_BRDF_INPUT SpecularSetup
            #pragma vertex vert_meta
            #pragma fragment frag_meta_ld

            #pragma shader_feature _EMISSION
            #pragma shader_feature _SPECGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature EDITOR_VISUALIZATION

            #include "UnityStandardMeta.cginc"
            #include "LightweightPipelineCore.cginc"

            fixed4 frag_meta_ld(v2f_meta i) : SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                o.Albedo = Albedo(i.uv);

                half4 specularColor;
                SpecularGloss(i.uv.xy, 1.0, specularColor);
                o.SpecularColor = specularColor;

#ifdef _EMISSION
                o.Emission += LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, i.uv).rgb) * _EmissionColor;
#else
                o.Emission += _EmissionColor;
#endif

                return UnityMetaFragment(o);
            }
            ENDCG
        }
    }
    Fallback "Standard (Specular setup)"
    CustomEditor "LightweightPipelineMaterialEditor"
}
