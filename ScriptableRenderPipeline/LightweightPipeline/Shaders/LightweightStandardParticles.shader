Shader "LightweightPipeline/Particles/Standard"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _MetallicGlossMap("Metallic", 2D) = "white" {}
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DistortionStrength("Strength", Float) = 1.0
        _DistortionBlend("Blend", Range(0.0, 1.0)) = 0.5

        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1.0
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1.0
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2.0

        // Hidden properties
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _FlipbookMode("__flipbookmode", Float) = 0.0
        [HideInInspector] _LightingEnabled("__lightingenabled", Float) = 1.0
        [HideInInspector] _DistortionEnabled("__distortionenabled", Float) = 0.0
        [HideInInspector] _EmissionEnabled("__emissionenabled", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _SoftParticlesEnabled("__softparticlesenabled", Float) = 0.0
        [HideInInspector] _CameraFadingEnabled("__camerafadingenabled", Float) = 0.0
        [HideInInspector] _SoftParticleFadeParams("__softparticlefadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _CameraFadeParams("__camerafadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _DistortionStrengthScaled("__distortionstrengthscaled", Float) = 0.0
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "IgnoreProjector" = "True" "PreviewType" = "Plane" "PerformanceChecks" = "False" "RenderPipeline" = "LightweightPipeline"}

        BlendOp[_BlendOp]
        Blend[_SrcBlend][_DstBlend]
        ZWrite[_ZWrite]
        Cull[_Cull]

        Pass
        {
            Tags {"LightMode" = "LightweightForward"}
            CGPROGRAM
            #pragma vertex ParticlesLitVertex
            #pragma fragment ParticlesLitFragment
            #pragma multi_compile __ SOFTPARTICLES_ON
            #pragma multi_compile _MAIN_DIRECTIONAL_LIGHT _MAIN_SPOT_LIGHT
            #pragma target 3.5

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _FADING_ON
            #pragma shader_feature _REQUIRE_UV2

            #define NO_LIGHTMAP
            #define NO_ADDITIONAL_LIGHTS

            #include "UnityStandardParticles.cginc"
            #include "LightweightLighting.cginc"

            struct VertexOutputLit
            {
                half4 color                     : COLOR;
                float2 texcoord                 : TEXCOORD0;
#if _NORMALMAP
                half3 tangent                   : TEXCOORD1;
                half3 binormal                  : TEXCOORD2;
                half3 normal                    : TEXCOORD3;
#else
                half3 normal                    : TEXCOORD1;
#endif

#if defined(_FLIPBOOK_BLENDING)
                float3 texcoord2AndBlend        : TEXCOORD4;
#endif
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
                float4 projectedPosition        : TEXCOORD5;
#endif
                float4 posWS                    : TEXCOORD6; // xyz: position; w = fogFactor;
                float4 clipPos                  : SV_POSITION;
            };

            void InitializeSurfaceData(VertexOutputLit IN, out SurfaceOutputStandard surfaceData)
            {
                Input input;
                input.color = IN.color;
                input.texcoord = IN.texcoord;
#if defined(_FLIPBOOK_BLENDING)
                input.texcoord2AndBlend = IN.texcoord2AndBlend;
#endif
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
                input.projectedPosition = IN.projectedPosition;
#endif

                // No distortion Support
                surfaceData.Normal = half3(0, 0, 1);
                surfaceData.Occlusion = 1.0;
                surf(input, surfaceData);
            }

            VertexOutputLit ParticlesLitVertex(appdata_particles v)
            {
                VertexOutputLit o;
                float4 clipPosition = UnityObjectToClipPos(v.vertex);

#if _NORMALMAP
                OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
#endif
                o.color = v.color;
                o.posWS.xyz = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.posWS.w = ComputeFogFactor(clipPosition.z);
                vertTexcoord(v, o);
                vertFading(o);
                o.clipPos = clipPosition;
                return o;
            }

            half4 ParticlesLitFragment(VertexOutputLit IN) : SV_Target
            {
                SurfaceOutputStandard surfaceData;
                InitializeSurfaceData(IN, surfaceData);

                float3 positionWS = IN.posWS.xyz;
                half3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - positionWS);
                half fogFactor = IN.posWS.w;

#if _NORMALMAP
                half3 normalWS = TangentToWorldNormal(surfaceData.Normal, IN.tangent, IN.binormal, IN.normal);
#else
                half3 normalWS = normalize(IN.normal);
#endif

                half3 zero = half3(0.0, 0.0, 0.0);
                half4 color = LightweightFragmentPBR(positionWS, normalWS, viewDirWS, /*indirectDiffuse*/ zero, /*vertex lighting*/ zero, surfaceData.Albedo,
                    surfaceData.Metallic, /* specularColor */ zero, surfaceData.Smoothness, surfaceData.Occlusion, surfaceData.Emission, surfaceData.Alpha);
                ApplyFog(color.rgb, fogFactor);
                return OUTPUT_COLOR(color);
            }
            ENDCG
        }
    }

    Fallback "LightweightPipeline/Unlit"
    CustomEditor "LightweightStandardParticlesShaderGUI"
}
