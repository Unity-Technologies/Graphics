#ifndef UNITY_VISUAL_EFFECT_MATRICES_OVERRIDE_INCLUDED
#define UNITY_VISUAL_EFFECT_MATRICES_OVERRIDE_INCLUDED

#ifndef HAVE_VFX_MODIFICATION
#error HAVE_VFX_MODIFICATION is expected at this point (ShaderGraph code generation for VFX)
#endif

#ifdef UNITY_SPACE_TRANSFORMS_INCLUDED
#error VisualEffectMatrices must be included *before* space transform
#endif

// Abstraction of Unity matrices for VFX element/particles.
#undef  UNITY_MATRIX_M
static float4x4 elementToWorld;
#define UNITY_MATRIX_M elementToWorld

#undef  UNITY_MATRIX_I_M
static float4x4 worldToElement;
#define UNITY_MATRIX_I_M worldToElement

#endif
