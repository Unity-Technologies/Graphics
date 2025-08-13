#ifndef SG_SELECTION_PICKING_PASS_INCLUDED
#define SG_SELECTION_PICKING_PASS_INCLUDED

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if (!defined(UNIVERSAL_TERRAIN_ENABLED) && defined(_ALPHATEST_ON)) || defined(_TERRAIN_SG_ALPHA_CLIP)
        // This isn't defined in the sprite passes. It looks like the built-in legacy shader will use this as it's default constant
        float alphaClipThreshold = 0.01f;
        #if ALPHA_CLIP_THRESHOLD
            alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
        #endif
        clip(surfaceDescription.Alpha - alphaClipThreshold);
    #endif

    half4 outColor = 0;
    #ifdef SCENESELECTIONPASS
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
    #elif defined(SCENEPICKINGPASS)
        outColor = unity_SelectionID;
    #endif

    return outColor;
}

#endif
