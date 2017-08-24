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

            #pragma multi_compile _ _SINGLE_DIRECTIONAL_LIGHT
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
                UNITY_SETUP_INSTANCE_ID(v);
                LightweightVertexOutput o = (LightweightVertexOutput)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv01.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef LIGHTMAP_ON
                o.uv01.zw = v.lightmapUV * unity_LightmapST.xy + unityLightmapST.wz;
#endif

                half3 eyeVec = normalize(_WorldSpaceCameraPos - posWorld.xyz);
                half3 normalWorld = UnityObjectToWorldNormal(v.normal);

                o.normalWorld.xyz = normalWorld;
                o.eyeVec.xyz = eyeVec;

#ifdef _NORMALMAP
                half3 tangentSpaceEyeVec;
                TangentSpaceLightingInput(normalWorld, v.tangent, _WorldSpaceLightPos0.xyz, eyeVec, o.tangentSpaceLightDir, tangentSpaceEyeVec);
#if SPECULAR_HIGHLIGHTS
                o.tangentSpaceEyeVec = tangentSpaceEyeVec;
#endif
#endif

                // TODO:
                //TRANSFER_SHADOW(o);

                o.fogCoord.yzw = max(half3(0, 0, 0), ShadeSH9(half4(normalWorld, 1)));

                o.normalWorld.w = Pow4(1 - saturate(dot(normalWorld, eyeVec))); // fresnel term
#if !GLOSSMAP
                o.eyeVec.w = saturate(_Glossiness + MetallicSetup_Reflectivity()); // grazing term
#endif

                UNITY_TRANSFER_FOG(o, o.pos);
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

                half3 diffColor = DiffuseAndSpecularFromMetallic(albedo, metallicGloss.x, /*out*/ specColor, /*out*/ oneMinusReflectivity);

    #if defined(_NORMALMAP)
                half3 tangentSpaceNormal = NormalInTangentSpace(i.uv01.xy);
                half3 reflectVec = reflect(-i.tangentSpaceEyeVec, tangentSpaceNormal);
    #else
                half3 reflectVec = reflect(-i.eyeVec, i.normalWorld.xyz);
    #endif

                // perceptualRoughness
                Unity_GlossyEnvironmentData g;
                g.roughness = 1 - smoothness;
                g.reflUVW = reflectVec;

                // TODO: shader keyword for occlusion
                // TODO: Reflection Probe blend support.
                // GI
                UnityIndirect indirectLight;
                half occlusion = Occlusion(uv);
                indirectLight.diffuse = (DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV)) + i.fogCoord.yzw) * occlusion;
                indirectLight.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g) * occlusion;

                // PBS
    #if GLOSSMAP
                half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
    #else
                half grazingTerm = i.eyeVec.w;
    #endif

                half perVertexFresnelTerm = i.normalWorld.w;

                half3 color = LightweightBRDFIndirect(diffColor, specColor, indirectLight, grazingTerm, perVertexFresnelTerm);

                // TODO: SPOT & POINT keywords
    #ifdef _SINGLE_DIRECTIONAL_LIGHT
                UnityLight mainLight;
                mainLight.color = _LightColor0.rgb;
                mainLight.dir = _LightPosition0.xyz;

    #if defined(_NORMALMAP)
                half NdotL = saturate(dot(s.tangentSpaceNormal, i.tangentSpaceLightDir));
    #else
                half NdotL = saturate(dot(i.normalWorld.xyz, mainLight.dir));
    #endif

                // TODO: Atten/Shadow
                half atten = 1;
                half RdotL = dot(reflectVec, mainLight.dir);
                half3 attenuatedLightColor = mainLight.color * NdotL;

                color += LightweightBRDFDirect(diffColor, specColor, smoothness, RdotL) * attenuatedLightColor;
    #else
                // TODO: LIGHTLOOP
                color = half3(0, 1, 0);
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

