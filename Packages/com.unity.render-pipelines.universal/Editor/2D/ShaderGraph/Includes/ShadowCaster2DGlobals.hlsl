#ifndef SHADOW_CASTER_2D_GLOBALS_INCLUDED
#define SHADOW_CASTER_2D_GLOBALS_INCLUDED

// Pre-graph declarations for the ShadowCaster2D SubTarget.
//
// Everything a generated node function might reference has to be declared pre-graph, because
// CodeFunctionNode bodies are emitted at file scope between the pre-graph and post-graph includes.
// That is also why the SOFT_SHADOW_EXTERNAL_ATTRIBUTES define lives here rather than in the pass
// include: it has to precede SoftShadowProjectVertex.hlsl, and IncludeCollection order is the only
// thing that guarantees that.

// ShaderGraph emits its own `Attributes` and `Varyings` structs inside every pass's HLSLPROGRAM, so
// the soft shadow header must not bring its semantic-bearing versions into scope -- two declarations
// of either name in one translation unit is a redefinition error. With this defined, the header
// exposes only SoftShadowPayload and SoftShadowProject, which is what the pass include calls.
#define SOFT_SHADOW_EXTERNAL_ATTRIBUTES 1

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SoftShadowProjectVertex.hlsl"

// UnityFlipSprite / SetUpSpriteInstanceProperties / unity_SpriteProps for the sprite caster variant.
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

// Set per caster by ShadowRendering (from ShadowCaster2D.alphaCutoff), so it is component state rather
// than anything the graph should restate. Declared here because the SubTarget does not include
// Shadow2DCasterVertex.hlsl -- that header carries its own Attributes/Varyings and would collide for
// the same reason as above.
float _ShadowAlphaCutoff;

// The light's bounding radius, set alongside _LightPos by ShadowRendering. Declared here rather than in
// SoftShadowProjectVertex.hlsl because ShadowProjectVertex.hlsl (the Legacy header) already declares
// it -- keeping it SubTarget-scoped means no shader can end up with two declarations. Read by the
// Active Shadow2D Light node.
float _ShadowRadius;

// The sprite caster's texture and tint, matching Shadow2DCasterVertex.hlsl. Coverage is reproduced
// from the built-in shader rather than authored, so these are the shader's own uniforms and not graph
// properties -- CollectShaderProperties adds the Properties-block entries with DoNotDeclare so the
// SpriteRenderer can still bind _MainTex without a duplicate HLSL declaration.
#if defined(SHADOW_SPRITE_CASTER)
sampler2D _MainTex;
float4    _MainTex_ST;
float4    _Color;
#endif

#endif // SHADOW_CASTER_2D_GLOBALS_INCLUDED
