// No support to Distortion
// No support to Shadows
Shader "LightweightPipeline/Particles/Standard Unlit"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

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
                Name "ParticlesUnlit"
                HLSLPROGRAM
                // Required to compile gles 2.0 with standard srp library
                #pragma prefer_hlslcc gles
                #pragma exclude_renderers d3d11_9x
                #pragma multi_compile __ SOFTPARTICLES_ON
                #pragma multi_compile_fog
                #pragma target 2.0

                #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
                #pragma shader_feature _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON
                #pragma shader_feature _EMISSION
                #pragma shader_feature _FADING_ON
                #pragma shader_feature _REQUIRE_UV2

                #pragma vertex vertParticleUnlit
                #pragma fragment fragParticleUnlit

                #include "LWRP/ShaderLibrary/Particles.hlsl"

                VertexOutputLit vertParticleUnlit(appdata_particles v)
                {
                    VertexOutputLit o = (VertexOutputLit)0;

                    // position ws is used to compute eye depth in vertFading
                    o.posWS.xyz = TransformObjectToWorld(v.vertex.xyz);
                    o.posWS.w = ComputeFogFactor(o.clipPos.z);
                    o.clipPos = TransformWorldToHClip(o.posWS.xyz);
                    o.color = v.color;

                    // TODO: Instancing
                    //vertColor(o.color);
                    vertTexcoord(v, o);
                    vertFading(o, o.posWS, o.clipPos);

                    return o;
                }

                half4 fragParticleUnlit(VertexOutputLit IN) : SV_Target
                {
                    half4 albedo = SampleAlbedo(IN, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
                    half3 diffuse = AlphaModulate(albedo.rgb, albedo.a);
                    half alpha = AlphaBlendAndTest(albedo.a, _Cutoff);
                    half3 emission = SampleEmission(IN, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));

                    half3 result = diffuse + emission;
                    half fogFactor = IN.posWS.w;
                    ApplyFogColor(result, half3(0, 0, 0), fogFactor);
                    return half4(result, alpha);
                }
                ENDHLSL
            }
        }
    }

    CustomEditor "LightweightStandardParticlesShaderGUI"
}
