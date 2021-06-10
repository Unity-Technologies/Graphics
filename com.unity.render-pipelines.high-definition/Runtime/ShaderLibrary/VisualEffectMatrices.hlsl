#ifndef UNITY_VISUAL_EFFECT_MATRICES_INCLUDED
#define UNITY_VISUAL_EFFECT_MATRICES_INCLUDED
#ifdef  HAVE_VFX_MODIFICATION

// Abstraction of Unity matrices for VFX element/particles.
#undef  UNITY_MATRIX_M
static float4x4 elementToWorld;
#define UNITY_MATRIX_M elementToWorld

#undef  UNITY_MATRIX_I_M
static float4x4 worldToElement;
#define UNITY_MATRIX_I_M worldToElement

#endif
#endif
