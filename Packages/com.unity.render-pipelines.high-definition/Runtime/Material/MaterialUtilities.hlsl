// Using this function instead of accessing the constant directly allows for overrides, in particular
// in Path Tracing where we want to change the sidedness behaviour based on the transparency mode.
float3 GetDoubleSidedConstants()
{
#ifdef _DOUBLESIDED_ON

    #if (SHADERPASS == SHADERPASS_PATH_TRACING)

        #if defined(_SURFACE_TYPE_TRANSPARENT) && (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE))
            return 1.0; // Force to 'None'
        #else
            return _DoubleSidedConstants.z > 0.0 ? -1.0 : _DoubleSidedConstants.xyz; // Force to 'Flip' or 'Mirror'
        #endif

    #else // SHADERPASS_PATH_TRACING

        return _DoubleSidedConstants.xyz;

    #endif // SHADERPASS_PATH_TRACING

#else // _DOUBLESIDED_ON

    return 1.0;

#endif // _DOUBLESIDED_ON
}

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

#if defined(SURFACE_GRADIENT) || defined(DECAL_NORMAL_BLENDING)
void GetNormalWS_SG(FragInputs input, float3 normalTS, out float3 normalWS, float3 doubleSidedConstants)
{
#ifdef _DOUBLESIDED_ON
    // Flip the displacements (the entire surface gradient) in the 'flip normal' mode.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    normalTS *= flipSign;
#endif

    normalWS = SurfaceGradientResolveNormal(input.tangentToWorld[2], normalTS);
}
#endif

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalWS(FragInputs input, float3 normalTS, out float3 normalWS, float3 doubleSidedConstants)
{
#if defined(SURFACE_GRADIENT)
    GetNormalWS_SG(input, normalTS, normalWS, doubleSidedConstants);
#else

    #ifdef _DOUBLESIDED_ON
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    normalTS.xy *= flipSign;
    #endif // _DOUBLESIDED_ON

    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalized)
    normalWS = SafeNormalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
#endif
}

// This function takes a world space src normal + applies a correction to the normal if it is not pointing towards the near plane.
void GetNormalWS_SrcWS(FragInputs input, float3 srcNormalWS, out float3 normalWS, float3 doubleSidedConstants)
{
#ifdef _DOUBLESIDED_ON
    srcNormalWS = (!input.isFrontFace && doubleSidedConstants.z < 0) ? srcNormalWS + 2 * input.tangentToWorld[2] * max(0, -dot(input.tangentToWorld[2], srcNormalWS)) : srcNormalWS;
    normalWS = (!input.isFrontFace && doubleSidedConstants.x < 0) ? reflect(-srcNormalWS, input.tangentToWorld[2]) : srcNormalWS;
#else
    normalWS = srcNormalWS;
#endif
}

// This function converts an object space normal to world space + applies a correction to the normal if it is not pointing towards the near plane.
void GetNormalWS_SrcOS(FragInputs input, float3 srcNormalOS, out float3 normalWS, float3 doubleSidedConstants)
{
    float3 srcNormalWS = TransformObjectToWorldNormal(srcNormalOS);
    GetNormalWS_SrcWS(input, srcNormalWS, normalWS, doubleSidedConstants);
}
