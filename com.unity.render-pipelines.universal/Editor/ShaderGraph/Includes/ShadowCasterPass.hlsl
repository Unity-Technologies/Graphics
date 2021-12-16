#ifndef SG_SHADOW_PASS_INCLUDED
#define SG_SHADOW_PASS_INCLUDED

PackedVaryings vert
(
#ifdef BRG_DRAW_PROCEDURAL
    uint vertexID : SV_VertexID
#else
    Attributes input
#endif
)
{
    Varyings output = (Varyings)0;

#ifdef BRG_DRAW_PROCEDURAL

    Attributes input = (Attributes)0;

    input.positionOS = LoadBRGProcedural_Position(vertexID);

#ifdef ATTRIBUTES_NEED_NORMAL
    input.normalOS = LoadBRGProcedural_Normal(vertexID);
#endif

#ifdef ATTRIBUTES_NEED_TANGENT
    input.tangentOS = LoadBRGProcedural_Tangent(vertexID);
#endif

#if UNITY_ANY_INSTANCING_ENABLED
    input.instanceID = unity_InstanceID;
#endif

#endif

    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    #if _ALPHATEST_ON
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    return 0;
}

#endif
