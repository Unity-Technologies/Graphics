//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "HDRP/Material/MaterialUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    // TODO: Remove this zero initialize once we have written all the code
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    surfaceData.normalWS = input.worldToTangent[2].xyz;
    
    float2 baseColorMapUv = TRANSFORM_TEX(input.texCoord0, _BaseColorMap);
    surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, baseColorMapUv).rgb * _BaseColor.rgb;
    float alpha = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, baseColorMapUv).a * _BaseColor.a;

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, baseColorMapUv, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
    }
#endif

    // -------------------------------------------------------------
    // Builtin Data
    // -------------------------------------------------------------

    // For back lighting we use the oposite vertex normal 
    InitBuiltinData(alpha, surfaceData.normalWS, -input.worldToTangent[2], input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
