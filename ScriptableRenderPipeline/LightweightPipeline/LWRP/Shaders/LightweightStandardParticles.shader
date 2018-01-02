// ------------------------------------------
// Only directional light is supported for lit particles
// No shadow
// No distortion
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

        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1.0
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1.0
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2.0

        // Hidden properties
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _FlipbookMode("__flipbookmode", Float) = 0.0
        [HideInInspector] _LightingEnabled("__lightingenabled", Float) = 1.0
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
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex ParticlesLitVertex
            #pragma fragment ParticlesLitFragment
            #pragma multi_compile __ SOFTPARTICLES_ON
            #pragma target 3.5

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _FADING_ON
            #pragma shader_feature _REQUIRE_UV2

            #include "LightweightShaderLibrary/Particles.hlsl"
            #include "LightweightShaderLibrary/Lighting.hlsl"

            VertexOutputLit ParticlesLitVertex(appdata_particles v)
            {
                VertexOutputLit o;
                float4 clipPosition = TransformObjectToHClip(v.vertex.xyz);

#if _NORMALMAP
                OutputTangentToWorld(v.tangent, v.normal, o.tangent, o.binormal, o.normal);
#else
                o.normal = normalize(TransformObjectToWorldNormal(v.normal));
#endif
                o.color = v.color;
                o.posWS.xyz = TransformObjectToWorld(v.vertex.xyz).xyz;
                o.clipPos = TransformWorldToHClip(o.posWS.xyz);
                o.posWS.w = ComputeFogFactor(o.clipPos.z);
                vertTexcoord(v, o);
                vertFading(o);
                o.clipPos = clipPosition;
                return o;
            }

            half4 ParticlesLitFragment(VertexOutputLit IN) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeSurfaceData(IN, surfaceData);

                float3 positionWS = IN.posWS.xyz;
                half3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos - positionWS);
                half fogFactor = IN.posWS.w;

#if _NORMALMAP
                half3 normalWS = TangentToWorldNormal(surfaceData.normal, IN.tangent, IN.binormal, IN.normal);
#else
                half3 normalWS = normalize(IN.normal);
#endif

                half3 zero = half3(0.0, 0.0, 0.0);
                half4 color = LightweightFragmentPBR(positionWS, normalWS, viewDirWS, /*indirectDiffuse*/ zero, /*vertex lighting*/ zero, surfaceData.albedo,
                    surfaceData.metallic, /* specularColor */ zero, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
                ApplyFog(color.rgb, fogFactor);
                return color;
            }

            ENDHLSL
        }
    }

    Fallback "LightweightPipeline/Particles/Standard Unlit"
    CustomEditor "LightweightStandardParticlesShaderGUI"
}
