// Pre-graph include for UniversalLight2DSubTarget.
//
// Declares the pipeline-owned globals that BOTH the graph body (via user Custom Function
// nodes) and the post-graph vert/frag in Light2DPass.hlsl reference. Kept pre-graph so any
// node function emitted at file scope by CodeFunctionNode / IGeneratesFunction sees the
// symbols already declared.

// Per-light data (constant-buffer path; USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA == 0
// in LightingUtility.hlsl). Expands to L2DColor / L2DPosition / L2DFalloffIntensity /
// L2DFalloffDistance / _L2D_SHADOW_INTENSITY / _L2D_VOLUME_OPACITY / _L2D_LIGHT_TYPE /
// _L2D_LIGHT_RENDERING_LAYER etc., accessed via the _L2D_* aliases.
UNITY_LIGHT2D_DATA

// Falloff LUT. NOT part of UNITY_LIGHT2D_DATA — declared alongside it because Light2DPass.hlsl
// samples it for the built-in Point falloff, and so a graph reproducing that convention can
// reference _FalloffLookup from a Custom Function node without needing a per-node include.
TEXTURE2D(_FalloffLookup);
SAMPLER(sampler_FalloffLookup);

// Point-light distance/angle/direction lookup LUT — set globally by the URP 2D
// GlobalPropertiesPass. Declared here so Light2DPass.hlsl can sample it to reproduce
// Hidden/Light2D's Point (Spot) falloff pipeline-side without requiring the graph
// author to wire it up. Mirrors the declaration inside Shaders/2D/Light2D.shader.
TEXTURE2D(_LightLookup);
SAMPLER(sampler_LightLookup);
half4 _LightLookup_TexelSize;

// Sprite-light cookie texture — bound per-draw by RendererLighting.SetCookieShaderProperties
// via the property block when a Sprite Light2D has a cookie sprite assigned (see
// k_CookieTexID in RendererLighting.cs). Unlike _PointLightCookieTex, this is declared
// unconditionally to mirror Hidden/Light2D (Shaders/2D/Light2D.shader:55-56) — the Sprite
// branch in Light2DPass.hlsl samples it based on a runtime _L2D_LIGHT_TYPE check rather
// than a compile-time keyword, and an unbound _CookieTex samples as white which is a
// safe no-op multiplier for non-Sprite variants.
TEXTURE2D(_CookieTex);
SAMPLER(sampler_CookieTex);

// Point-light cookie texture — bound per-draw by RendererLighting.SetCookieShaderProperties
// via the property block when a Point Light2D has a cookie sprite assigned. Guarded on
// USE_POINT_LIGHT_COOKIES so cookie-less variants don't reference an unbound sampler.
// Mirrors the declaration inside Shaders/2D/Light2D.shader.
#if USE_POINT_LIGHT_COOKIES
TEXTURE2D(_PointLightCookieTex);
SAMPLER(sampler_PointLightCookieTex);
#endif

// Normal map textures. Only expands when USE_NORMAL_MAP is on — see LightingUtility.hlsl.
// Declared pre-graph so any node function that references _NormalMap sees it declared.
NORMALS_LIGHTING_VARIABLES

// Shadow map textures. Declared pre-graph (not in Light2DPass.hlsl) so node functions
// such as the Shadow 2D node can reference _ShadowTex at file scope before the
// post-graph include runs.
SHADOW_VARIABLES

// Rendering-layer mask receiver texture.
TYPED_TEXTURE2D_X(uint, _CameraRenderingLayersTexture);

// HDR emulation scale. In preview the pipeline never sets this global so it would
// default to 0 and swallow the output; use a static const instead. In runtime
// the global is set per-draw by URP's 2D renderer.
#if defined(SHADERGRAPH_PREVIEW_MAIN) || defined(SHADERGRAPH_PREVIEW)
    static const half _InverseHDREmulationScale = 1.0;
#else
    half _InverseHDREmulationScale;
#endif

// -----------------------------------------------------------------------------
// Preview overrides for pipeline globals
// -----------------------------------------------------------------------------
// In preview the Light2D component never runs, so all _L2D_* globals are zero.
// Redefine the accessor aliases to safe literal constants so the graph's output
// is visible rather than multiplied to black.
#if defined(SHADERGRAPH_PREVIEW_MAIN) || defined(SHADERGRAPH_PREVIEW)
    #undef  _L2D_COLOR
    #define _L2D_COLOR                  half4(1, 1, 1, 1)
    #undef  _L2D_POSITION
    #define _L2D_POSITION               float4(0, 0, 0, 0)
    #undef  _L2D_FALLOFF_INTENSITY
    #define _L2D_FALLOFF_INTENSITY      half(0.5)
    #undef  _L2D_FALLOFF_DISTANCE
    #define _L2D_FALLOFF_DISTANCE       half(0.0)
    #undef  _L2D_SHADOW_INTENSITY
    #define _L2D_SHADOW_INTENSITY       half(1.0)
    #undef  _L2D_VOLUME_OPACITY
    #define _L2D_VOLUME_OPACITY         half(1.0)
    #undef  _L2D_LIGHT_TYPE
    #define _L2D_LIGHT_TYPE             5
    #undef  _L2D_LIGHT_RENDERING_LAYER
    #define _L2D_LIGHT_RENDERING_LAYER  uint(0xFFFFFFFFu)
#endif
