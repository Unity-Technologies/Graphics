#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

CBUFFER_START(AccumulationMotionBlurUniformBuffer)
float4 _AccumulationParams;
CBUFFER_END

#define _AccumulationWeight      _AccumulationParams.x
#define _AccumulationSampleCount _AccumulationParams.y
#define _AccumulationSampleIndex _AccumulationParams.z
#define _AccumulationNormalize   _AccumulationParams.w