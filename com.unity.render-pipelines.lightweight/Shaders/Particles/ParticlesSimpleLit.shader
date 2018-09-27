// ------------------------------------------
// Only directional light is supported for lit particles
// No shadow
// No distortion
Shader "Lightweight Render Pipeline/Particles/Simple Lit"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Shininess("Shininess", Range(0.01, 1.0)) = 1.0
        _GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0

        _Glossiness("Glossiness", Range(0.0, 1.0)) = 0.5
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        [HideInInspector] _SpecSource("Specular Color Source", Float) = 0.0
        _SpecColor("Specular", Color) = (1.0, 1.0, 1.0)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [HideInInspector] _GlossinessSource("Glossiness Source", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

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
            Name "ForwardLit"
            Tags {"LightMode" = "LightweightForward"}
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex ParticlesLitVertex
            #pragma fragment ParticlesLitFragment
            #pragma multi_compile __ SOFTPARTICLES_ON
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature _ _SPECGLOSSMAP _SPECULAR_COLOR
            #pragma shader_feature _ _GLOSSINESS_FROM_BASE_ALPHA
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _FADING_ON
            #pragma shader_feature _REQUIRE_UV2

            #define BUMP_SCALE_NOT_SUPPORTED 1

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Particles.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

            VaryingsParticle ParticlesLitVertex(AttributesParticle input)
            {
                VaryingsParticle output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal
#if defined(_NORMALMAP)
                    , input.tangent
#endif
                );
                half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
#if !SHADER_HINT_NICE_QUALITY
                viewDirWS = SafeNormalize(viewDirWS);
#endif

                output.normal = normalInput.normalWS;
#ifdef _NORMALMAP
                output.tangent = normalInput.tangentWS;
                output.bitangent = normalInput.bitangentWS;
#endif

                output.posWS.xyz = vertexInput.positionWS.xyz;
                output.posWS.w = ComputeFogFactor(vertexInput.positionCS.z);
                output.clipPos = vertexInput.positionCS;
                output.viewDirShininess = half4(viewDirWS, _Shininess * 128.0h);
                output.color = input.color;

                // TODO: Instancing
                // vertColor(output.color);
                vertTexcoord(input, output);
                vertFading(output, vertexInput.positionWS, vertexInput.positionCS);
                return output;
            }

            half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
            {
                half4 albedo = SampleAlbedo(input, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
                half3 diffuse = AlphaModulate(albedo.rgb, albedo.a);
                half alpha = AlphaBlendAndTest(albedo.a, _Cutoff);
                half3 normalTS = SampleNormalTS(input, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
                half3 emission = SampleEmission(input, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));
                half4 specularGloss = SampleSpecularGloss(input, albedo.a, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap));
                half shininess = input.viewDirShininess.w;

                InputData inputData;
                InitializeInputData(input, normalTS, inputData);

                half4 color = LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }

            ENDHLSL
        }
    }

    Fallback "Lightweight Render Pipeline/Particles/Unlit"
    CustomEditor "UnityEditor.Experimental.Rendering.LightweightPipeline.ParticlesLitShaderGUI"
}
