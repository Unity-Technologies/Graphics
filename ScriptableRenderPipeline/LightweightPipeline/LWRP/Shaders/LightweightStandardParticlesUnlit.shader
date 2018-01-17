// No support to Distortion
// No support to Shadows
Shader "LightweightPipeline/Particles/Standard Unlit"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

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
        [HideInInspector] _ColorMode("__colormode", Float) = 0.0
        [HideInInspector] _FlipbookMode("__flipbookmode", Float) = 0.0
        [HideInInspector] _LightingEnabled("__lightingenabled", Float) = 0.0
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
        [HideInInspector] _ColorAddSubDiff("__coloraddsubdiff", Vector) = (0,0,0,0)
    }

    Category
    {
        SubShader
        {
            Tags{"RenderType" = "Opaque" "IgnoreProjector" = "True" "PreviewType" = "Plane" "PerformanceChecks" = "False"}

            BlendOp[_BlendOp]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]
            ColorMask RGB

            Pass
            {
                HLSLPROGRAM
                // Required to compile gles 2.0 with standard srp library
                #pragma prefer_hlslcc gles
                #pragma multi_compile __ SOFTPARTICLES_ON
                #pragma multi_compile_fog
                #pragma target 2.5

                #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
                #pragma shader_feature _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON
                #pragma shader_feature _NORMALMAP
                #pragma shader_feature _EMISSION
                #pragma shader_feature _FADING_ON
                #pragma shader_feature _REQUIRE_UV2

                #pragma vertex vertParticleUnlit
                #pragma fragment fragParticleUnlit

                #include "LWRP/ShaderLibrary/Particles.hlsl"
                #include "LWRP/ShaderLibrary/Core.hlsl"

                VertexOutputLit vertParticleUnlit(appdata_particles v)
                {
                    VertexOutputLit o = (VertexOutputLit)0;

                    // position ws is used to compute eye depth in vertFading
                    o.posWS.xyz = TransformObjectToWorld(v.vertex.xyz);
                    o.posWS.w = ComputeFogFactor(o.clipPos.z);
                    o.clipPos = TransformWorldToHClip(o.posWS.xyz);
                    o.color = v.color;

                    vertColor(o.color);
                    vertTexcoord(v, o);
                    vertFading(o, o.posWS, o.clipPos);

                    return o;
                }

                half4 fragParticleUnlit(VertexOutputLit IN) : SV_Target
                {
                    half4 albedo = readTexture(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), IN);
                    albedo *= _Color;

                    fragColorMode(IN);
                    fragSoftParticles(IN);
                    fragCameraFading(IN);

        #if defined(_NORMALMAP)
                    float3 normal = normalize(UnpackNormalScale(readTexture(TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap), IN), _BumpScale));
        #else
                    float3 normal = float3(0,0,1);
        #endif

        #if defined(_EMISSION)
                    half3 emission = readTexture(TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap), IN).rgb;
        #else
                    half3 emission = 0;
        #endif

                    half4 result = albedo;

        #if defined(_ALPHAMODULATE_ON)
                    result.rgb = lerp(half3(1.0, 1.0, 1.0), albedo.rgb, albedo.a);
        #endif

                    result.rgb += emission * _EmissionColor.rgb;

        #if !defined(_ALPHABLEND_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAOVERLAY_ON)
                    result.a = 1;
        #endif

        #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff + 0.0001);
        #endif

                    half fogFactor = IN.posWS.w;
                    ApplyFogColor(result.rgb, half3(0, 0, 0), fogFactor);
                    return result;
                }
                ENDHLSL
            }
        }
    }

    CustomEditor "LightweightStandardParticlesShaderGUI"
}
