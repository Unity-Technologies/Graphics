// ------------------------------------------
// Only directional light is supported for lit particles
// No shadow
// No distortion
Shader "Lightweight Render Pipeline/Particles/Lit"
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
            Name "ForwardLit"
            Tags {"LightMode" = "LightweightForward"}
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex ParticlesLitVertex
            #pragma fragment ParticlesLitFragment
            #pragma multi_compile __ SOFTPARTICLES_ON
            #pragma target 2.0

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _EMISSION
            #pragma shader_feature _FADING_ON
            #pragma shader_feature _REQUIRE_UV2

            #include "ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

            VaryingsParticle ParticlesLitVertex(AttributesParticle input)
            {
                VaryingsParticle output = (VaryingsParticle)0;
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
#ifdef _NORMAL_MAP
                output.tangent = normalInput.tangentWS;
                output.bitangent = normalInput.bitangentWS;
#endif

                output.posWS.xyz = vertexInput.positionWS;
                output.posWS.w = ComputeFogFactor(vertexInput.positionCS.z);
                output.clipPos = vertexInput.positionCS;
                output.viewDirShininess = half4(viewDirWS, 0.0);
                output.color = input.color;

                // TODO: Instancing
                // vertColor(output.color);
                vertTexcoord(input, output);
                vertFading(output, vertexInput.positionWS, vertexInput.positionCS);
                return output;
            }

            half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeSurfaceData(input, surfaceData);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo,
                    surfaceData.metallic, half3(0, 0, 0), surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }

            ENDHLSL
        }
    }

    Fallback "Lightweight Render Pipeline/Particles/SimpleLit"
    CustomEditor "UnityEditor.Experimental.Rendering.LightweightPipeline.ParticlesLitShaderGUI"
}
