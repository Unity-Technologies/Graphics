// Shader targeted for low end devices. Single Pass Forward Rendering. Shader Model 2
Shader "ScriptableRenderPipeline/LightweightPipeline/FastBlinn"
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

#define VERTEX_CUSTOM \
	o.tangent = normalize(UnityObjectToWorldDir(v.tangent)); \
	o.binormal = cross(o.normal, o.tangent) * v.tangent.w;

#define VERTOUTPUT_CUSTOM \
	half3 tangent : TEXCOORD5; \
	half3 binormal : TEXCOORD6;

            #include "CGIncludes/LightweightFastBlinn.cginc"

			#pragma vertex Vertex
            #pragma fragment LightweightFragmentFastBlinn

			void DefineSurface(VertOutput i, inout SurfaceFastBlinn s)
			{
				// Albedo
				float4 c = tex2D(_MainTex, i.meshUV0.xy);
				s.Diffuse = LIGHTWEIGHT_GAMMA_TO_LINEAR(c.rgb) * _Color.rgb;
				// Specular
#ifdef _SPECGLOSSMAP
				half4 specularMap = tex2D(_SpecGlossMap, i.uv01.xy);
#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
				s.Specular = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
#endif
#elif defined(_SPECGLOSSMAP_BASE_ALPHA)
				s.Specular.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_SpecGlossMap, i.meshUV0.xy).rgb) * _SpecColor.rgb;
				s.Glossiness = s.Alpha;
#else
				s.Specular = _SpecColor.rgb;
				s.Glossiness = _SpecColor.a;
#endif
				// Shininess
				s.Shininess = _Shininess;
				// Normal
#if _NORMALMAP
				s.Normal = UnpackNormal(tex2D(_BumpMap, i.meshUV0.xy));
#endif
				// Emission
#ifndef _EMISSION
				s.Emission =  _EmissionColor.rgb;
#else
				s.Emission = LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, uv).rgb) * _EmissionColor.rgb;
#endif
				// Alpha
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
				s.Alpha = _Color.a;
#else
				s.Alpha = c.a * _Color.a;
#endif

#ifdef _ALPHATEST_ON
				clip(s.Alpha - _Cutoff);
#endif
			}

            ENDCG
        }

        Pass
        {
            Tags { "Lightmode" = "ShadowCaster" }
            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 2.0
            #include "UnityCG.cginc"
			#include "CGIncludes/LightweightPass.cginc"
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            ENDCG
        }

        Pass
        {
            Tags{"Lightmode" = "DepthOnly"}
            ZWrite On

            CGPROGRAM
            #pragma target 2.0
			#include "CGIncludes/LightweightPass.cginc"
            #pragma vertex depthVert
            #pragma fragment depthFrag
            ENDCG
        }
		/*
        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Tags{ "LightMode" = "Meta" }

            Cull Off

            CGPROGRAM
            #define UNITY_SETUP_BRDF_INPUT SpecularSetup

			#pragma shader_feature _EMISSION
            #pragma shader_feature _SPECGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature EDITOR_VISUALIZATION
			
			#include "UnityCG.cginc"
			#include "CGIncludes/LightweightPass.cginc"
            #pragma vertex Vert_Meta
            #pragma fragment frag_meta_ld

			void DefineSurfaceMeta(VertOutput_Meta i, inout SurfaceFastBlinn s)
			{
				// Albedo
				float4 c = tex2D(_MainTex, i.meshUV0.xy);
				s.Diffuse = c.rgb;
				// Specular
#ifdef _SPECGLOSSMAP
				half4 specularMap = tex2D(_SpecGlossMap, i.meshUV0.xy);
#if defined(UNITY_COLORSPACE_GAMMA) && defined(LIGHTWEIGHT_LINEAR)
				s.Specular = LIGHTWEIGHT_GAMMA_TO_LINEAR(specularGloss.rgb);
#endif
#elif defined(_SPECGLOSSMAP_BASE_ALPHA)
				s.Specular.rgb = LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_SpecGlossMap, i.meshUV0.xy).rgb) * _SpecColor.rgb;
				s.Glossiness = s.Alpha;
#else
				s.Specular = _SpecColor.rgb;
				s.Glossiness = _SpecColor.a;
#endif
				// Emission
#ifndef _EMISSION
				s.Emission = _EmissionColor.rgb;
#else
				s.Emission = LIGHTWEIGHT_GAMMA_TO_LINEAR(tex2D(_EmissionMap, uv).rgb) * _EmissionColor.rgb;
#endif
			}

            ENDCG
        }
		*/
    }
    Fallback "Standard (Specular setup)"
    CustomEditor "LightweightPipelineMaterialEditor"
}
