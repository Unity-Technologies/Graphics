#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Input.hlsl"

// Deprecated: A confusingly named and duplicate function that scales clipspace to unity NDC range. (-w < x(-y) < w --> 0 < xy < w)
// Use GetVertexPositionInputs().positionNDC instead for vertex shader
// Or a similar function in Common.hlsl, ComputeNormalizedDeviceCoordinatesWithZ()

#ifndef BUILTIN_TARGET_API
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}
#endif

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_DEPRECATED_INCLUDED
