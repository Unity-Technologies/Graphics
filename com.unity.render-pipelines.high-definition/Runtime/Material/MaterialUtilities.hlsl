// Flipping or mirroring a normal can be done directly on the tangent space. This has the benefit to apply to the whole process either in surface gradient or not.
// This function will modify FragInputs and this is not propagate outside of GetSurfaceAndBuiltinData(). This is ok as tangent space is not use outside of GetSurfaceAndBuiltinData().
void ApplyDoubleSidedFlipOrMirror(inout FragInputs input, float3 doubleSidedConstants)
{
#ifdef _DOUBLESIDED_ON
    // 'doubleSidedConstants' is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode.
    // It's float3(1, 1, 1) in the none mode.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.z;
    // For the 'Flip' mode, we should not modify the tangent and the bitangent (which correspond
    // to the surface derivatives), and instead modify (invert) the displacements.
    input.tangentToWorld[2] = flipSign * input.tangentToWorld[2]; // normal
#endif
}

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalWS(FragInputs input, float3 normalTS, out float3 normalWS, float3 doubleSidedConstants)
{
#ifdef SURFACE_GRADIENT

#ifdef _DOUBLESIDED_ON
    // Flip the displacements (the entire surface gradient) in the 'flip normal' mode.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    normalTS *= flipSign;
#endif

    normalWS = SurfaceGradientResolveNormal(input.tangentToWorld[2], normalTS);

#else // SURFACE_GRADIENT

#ifdef _DOUBLESIDED_ON
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    normalTS.xy *= flipSign;
#endif // _DOUBLESIDED_ON

    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.tangentToWorld));

#endif // SURFACE_GRADIENT
}
