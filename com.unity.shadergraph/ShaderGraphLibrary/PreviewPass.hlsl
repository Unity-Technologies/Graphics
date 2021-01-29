#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DotsDeformation.hlsl"

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
#if defined(DOTS_INSTANCING_ON)
    FetchComputeVertexData(input.positionOS.xyz, input.normalOS, input.tangentOS, input.vertexID);
#endif
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET 
{    
    Varyings unpacked = UnpackVaryings(packedInput);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    return surfaceDescription.Out;
}
