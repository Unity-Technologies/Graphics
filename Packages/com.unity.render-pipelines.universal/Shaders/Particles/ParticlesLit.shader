// ------------------------------------------
// No shadows
Shader "Universal Render Pipeline/Particles/Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

        // -------------------------------------
        // Particle specific
        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1.0
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1.0
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2.0
        _DistortionBlend("Distortion Blend", Range(0.0, 1.0)) = 0.5
        _DistortionStrength("Distortion Strength", Float) = 1.0

        // -------------------------------------
        // Hidden properties - Generic
        _Surface("__surface", Float) = 0.0
        _Blend("__mode", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0


        // Particle specific
        _ColorMode("_ColorMode", Float) = 0.0
        [HideInInspector] _BaseColorAddSubDiff("_ColorMode", Vector) = (0,0,0,0)
        [ToggleOff] _FlipbookBlending("__flipbookblending", Float) = 0.0
        [ToggleUI] _SoftParticlesEnabled("__softparticlesenabled", Float) = 0.0
        [ToggleUI] _CameraFadingEnabled("__camerafadingenabled", Float) = 0.0
        [ToggleUI] _DistortionEnabled("__distortionenabled", Float) = 0.0
        [HideInInspector] _SoftParticleFadeParams("__softparticlefadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _CameraFadeParams("__camerafadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _DistortionStrengthScaled("Distortion Strength Scaled", Float) = 0.1

        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _FlipbookMode("flipbook", Float) = 0
        [HideInInspector] _Glossiness("gloss", Float) = 0
        [HideInInspector] _Mode("mode", Float) = 0
        [HideInInspector] _Color("color", Color) = (1,1,1,1)
    }

    HLSLINCLUDE

    //Particle shaders rely on "write" to CB syntax which is not supported by DXC
    #pragma never_use_dxc

    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "PerformanceChecks" = "False"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }


        // ------------------------------------------------------------------
        //  Forward pass.
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            BlendOp[_BlendOp]
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP

            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _SOFTPARTICLES_ON
            #pragma shader_feature_local _FADING_ON
            #pragma shader_feature_local _DISTORTION_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex ParticlesLitVertex
            #pragma fragment ParticlesLitFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitForwardPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  GBuffer pass.
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "GBuffer"
            Tags{"LightMode" = "UniversalGBuffer"}

            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Particle Keywords
            //#pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON
            //#pragma shader_feature _SOFTPARTICLES_ON
            //#pragma shader_feature _FADING_ON
            //#pragma shader_feature _DISTORTION_ON

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex ParticlesGBufferVertex
            #pragma fragment ParticlesGBufferFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitGbufferPass.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Depth Only pass.
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesDepthOnlyPass.hlsl"
            ENDHLSL
        }
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _NORMALMAP
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesDepthNormalsPass.hlsl"
            ENDHLSL
        }
        // ------------------------------------------------------------------
        //  Scene view outline pass.
        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            BlendOp Add
            Blend One Zero
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #define PARTICLES_EDITOR_META_PASS
            #pragma target 2.0

            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex vertParticleEditor
            #pragma fragment fragParticleSceneHighlight

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesEditorPass.hlsl"

            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Scene picking buffer pass.
        Pass
        {
            Name "ScenePickingPass"
            Tags{ "LightMode" = "Picking" }

            BlendOp Add
            Blend One Zero
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #define PARTICLES_EDITOR_META_PASS
            #pragma target 2.0

            // -------------------------------------
            // Particle Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _FLIPBOOKBLENDING_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup

            #pragma vertex vertParticleEditor
            #pragma fragment fragParticleScenePicking

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesEditorPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags{ "LightMode" = "Universal2D" }

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Simple Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.ParticlesLitShader"
}
