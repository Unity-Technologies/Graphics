//-------------------------------------------------------------------------------------
//  TODO
//-------------------------------------------------------------------------------------

// This method is supposed to be defined in the shader when a user is writing an unlit shader.
void BuildSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, PositionInputs posInput, inout SurfaceData surfaceData, inout BuiltinData builtinData);

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
{
    ZERO_INITIALIZE(BuiltinData, builtinData);
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    // This functioin is calling user code to initialize builtinData and surfaceData
    BuildSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#ifdef _ALPHATEST_ON
    GENERIC_ALPHA_TEST(alpha, builtinData.alphaClipTreshold);
#endif

#if defined(DEBUG_DISPLAY)
    // Light Layers are currently not used for the Unlit shader (because it is not lit)
    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
    // display in the light layers visualization mode, therefore we need the renderingLayers
    builtinData.renderingLayers = GetMeshRenderingLightLayer();
#endif

    ApplyDebugToBuiltinData(builtinData);

    RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
}
