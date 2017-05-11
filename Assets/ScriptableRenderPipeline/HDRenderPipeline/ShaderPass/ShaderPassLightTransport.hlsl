#if SHADERPASS != SHADERPASS_LIGHT_TRANSPORT
#error SHADERPASS_is_not_correctly_define
#endif

#include "ShaderLibrary/Color.hlsl"

CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;
CBUFFER_END


// This was not in constant buffer in original unity, so keep outiside. But should be in as ShaderRenderPass frequency
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

#include "VertMesh.hlsl"

PackedVaryingsToPS Vert(AttributesMesh inputMesh)
{
    VaryingsToPS output;

    // Output UV coordinate in vertex shader
    if (unity_MetaVertexControl.x)
        inputMesh.positionOS.xy = inputMesh.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;

    if (unity_MetaVertexControl.y)
        inputMesh.positionOS.xy = inputMesh.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;

    // Zero out the Z component. However, OpenGL right now needs to actually use the incoming vertex
    // position, so also take this opportunity to create a dependence on it.
    inputMesh.positionOS.z = inputMesh.positionOS.z > 0 ? 1.0e-4 : 0.0;

    float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
    output.vmesh.positionCS = TransformWorldToHClip(positionWS);
    output.vmesh.texCoord0 = inputMesh.uv0;
    output.vmesh.texCoord1 = inputMesh.uv1;

#if defined(VARYINGS_NEED_COLOR)
    output.vmesh.color = inputMesh.color;
#endif

    return PackVaryingsToPS(output);
}

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.unPositionSS.xy, _ScreenSize.zw);
    // No position and depth in case of light transport
    float3 V = float3(0, 0, 1); // No vector view in case of light transport

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two time. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.

    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    // TODO: No if / else in original code from Unity, why ? keep like original code but should be either diffuse or emissive
    if (unity_MetaFragmentControl.x)
    {
        // Apply diffuseColor Boost from LightmapSettings.
        // put abs here to silent a warning, no cost, no impact as color is assume to be positive.
        res.rgb = Clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }

    if (unity_MetaFragmentControl.y)
    {
        // TODO: THIS LIMIT MUST BE REMOVE, IT IS NOT HDR, change when RGB9e5 is here.
        // Do we assume here that emission is [0..1] ?
        res = PackEmissiveRGBM(lightTransportData.emissiveColor);
    }

    return res;
}
