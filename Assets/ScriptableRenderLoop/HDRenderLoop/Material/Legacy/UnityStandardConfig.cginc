#ifndef UNITY_STANDARD_CONFIG_INCLUDED
#define UNITY_STANDARD_CONFIG_INCLUDED

// Define Specular cubemap constants
#ifndef UNITY_SPECCUBE_LOD_EXPONENT
#define UNITY_SPECCUBE_LOD_EXPONENT (1.5)
#endif
#ifndef UNITY_SPECCUBE_LOD_STEPS
#define UNITY_SPECCUBE_LOD_STEPS (6)
#endif

// Energy conservation for Specular workflow is Monochrome. For instance: Red metal will make diffuse Black not Cyan
#ifndef UNITY_CONSERVE_ENERGY
#define UNITY_CONSERVE_ENERGY 1
#endif
#ifndef UNITY_CONSERVE_ENERGY_MONOCHROME
#define UNITY_CONSERVE_ENERGY_MONOCHROME 1
#endif

// "platform caps" defines: they are controlled from TierSettings (Editor will determine values and pass them to compiler)
// UNITY_SPECCUBE_BOX_PROJECTION:					TierSettings.reflectionProbeBoxProjection
// UNITY_SPECCUBE_BLENDING:							TierSettings.reflectionProbeBlending
// UNITY_ENABLE_DETAIL_NORMALMAP:					TierSettings.detailNormalMap
// UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS:	TierSettings.semitransparentShadows

// disregarding what is set in TierSettings, some features have hardware restrictions
// so we still add safety net, otherwise we might end up with shaders failing to compile

#if SHADER_TARGET < 30
	#undef UNITY_SPECCUBE_BOX_PROJECTION
	#undef UNITY_SPECCUBE_BLENDING
	#undef UNITY_ENABLE_DETAIL_NORMALMAP
#endif
#if (SHADER_TARGET < 30) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D11_9X) || defined (SHADER_API_PSP2)
	#undef UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS
#endif

#ifndef UNITY_SAMPLE_FULL_SH_PER_PIXEL
//If this is enabled then we should consider Light Probe Proxy Volumes(SHEvalLinearL0L1_SampleProbeVolume) in ShadeSH9
#define UNITY_SAMPLE_FULL_SH_PER_PIXEL 0
#endif

#ifndef UNITY_BRDF_GGX
#define UNITY_BRDF_GGX 1
#endif

// Orthnormalize Tangent Space basis per-pixel
// Necessary to support high-quality normal-maps. Compatible with Maya and Marmoset.
// However xNormal expects oldschool non-orthnormalized basis - essentially preventing good looking normal-maps :(
// Due to the fact that xNormal is probably _the most used tool to bake out normal-maps today_ we have to stick to old ways for now.
// 
// Disabled by default, until xNormal has an option to bake proper normal-maps.
#ifndef UNITY_TANGENT_ORTHONORMALIZE
#define UNITY_TANGENT_ORTHONORMALIZE 0
#endif


// Some extra optimizations

// On PVR GPU there is an extra cost for dependent texture readback, especially hitting texCUBElod
// These defines should be set as keywords or smth (at runtime depending on GPU).
// for now we keep the code but disable it, as we want more optimization/cleanup passes

#ifndef UNITY_OPTIMIZE_TEXCUBELOD
	#define UNITY_OPTIMIZE_TEXCUBELOD 0
#endif

// Simplified Standard Shader is off by default and should not be used for Legacy Shaders
#ifndef UNITY_STANDARD_SIMPLE
	#define UNITY_STANDARD_SIMPLE 0
#endif

// Setup a new define with meaniful name to know if we require world pos in fragment shader
#define UNITY_REQUIRE_FRAG_WORLDPOS (defined(UNITY_SPECCUBE_BOX_PROJECTION) || UNITY_LIGHT_PROBE_PROXY_VOLUME)

#endif // UNITY_STANDARD_CONFIG_INCLUDED
