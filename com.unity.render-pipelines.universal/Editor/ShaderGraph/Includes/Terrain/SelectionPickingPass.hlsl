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

#ifdef _ALPHATEST_ON
    ClipHoles(unpacked.texCoord0.xy);
#endif

    half4 outColor = 0;
    #ifdef SCENESELECTIONPASS
        // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
        outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
    #elif defined(SCENEPICKINGPASS)
        outColor = _SelectionID;
    #endif

    return outColor;
}

#endif
