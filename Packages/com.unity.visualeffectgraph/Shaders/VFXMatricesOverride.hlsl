#ifndef UNITY_VISUAL_EFFECT_MATRICES_OVERRIDE_INCLUDED
#define UNITY_VISUAL_EFFECT_MATRICES_OVERRIDE_INCLUDED

#ifndef HAVE_VFX_MODIFICATION
#error HAVE_VFX_MODIFICATION is expected at this point (ShaderGraph code generation for VFX)
#endif

#ifdef UNITY_SPACE_TRANSFORMS_INCLUDED
#error VisualEffectMatrices must be included *before* space transform
#endif

#ifdef  SHADER_STAGE_COMPUTE

#undef  UNITY_MATRIX_M
static float4x4 vfxLocalToWorld;
#ifdef MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
#define UNITY_MATRIX_M ApplyCameraTranslationToMatrix(vfxLocalToWorld)
#else
#define UNITY_MATRIX_M vfxLocalToWorld
#endif

#undef  UNITY_MATRIX_I_M
static float4x4 vfxWorldToLocal;
#ifdef MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
#define UNITY_MATRIX_I_M ApplyCameraTranslationToInverseMatrix(vfxWorldToLocal)
#else
#define UNITY_MATRIX_I_M vfxWorldToLocal
#endif

#else //SHADER_STAGE_COMPUTE

//Store the previous definition of UNITY_MATRIX_M/I_M
float4x4 GetSGVFXUnityObjectToWorldBackup()     { return UNITY_MATRIX_M; }
float4x4 GetSGVFXUnityWorldToObjectBackup()     { return UNITY_MATRIX_I_M; }

float4x4 GetSGVFXUnityObjectToWorld()     { return GetSGVFXUnityObjectToWorldBackup(); }
float4x4 GetSGVFXUnityWorldToObject()     { return GetSGVFXUnityWorldToObjectBackup(); }

// Abstraction of Unity matrices for VFX element/particles.
#undef  UNITY_MATRIX_M
static float4x4 elementToWorld;
#define UNITY_MATRIX_M elementToWorld

#undef  UNITY_MATRIX_I_M
static float4x4 worldToElement;
#define UNITY_MATRIX_I_M worldToElement

#endif //SHADER_STAGE_COMPUTE


#endif
