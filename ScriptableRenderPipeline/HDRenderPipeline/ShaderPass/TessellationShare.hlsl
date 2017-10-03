// AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
#define MAX_TESSELLATION_FACTORS 15.0

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors HullConstant(InputPatch<PackedVaryingsToDS, 3> input)
{
    VaryingsToDS varying0 = UnpackVaryingsToDS(input[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(input[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(input[2]);

    float3 p0 = varying0.vmesh.positionWS;
    float3 p1 = varying1.vmesh.positionWS;
    float3 p2 = varying2.vmesh.positionWS;

    float3 n0 = varying0.vmesh.normalWS;
    float3 n1 = varying1.vmesh.normalWS;
    float3 n2 = varying2.vmesh.normalWS;

    // ref: http://reedbeta.com/blog/tess-quick-ref/
    // x - 1->2 edge
    // y - 2->0 edge
    // z - 0->1 edge
    // w - inside tessellation factor
    float4 tf = GetTessellationFactors(p0, p1, p2, n0, n1, n2);
    TessellationFactors output;
    output.edge[0] = min(tf.x, MAX_TESSELLATION_FACTORS);
    output.edge[1] = min(tf.y, MAX_TESSELLATION_FACTORS);
    output.edge[2] = min(tf.z, MAX_TESSELLATION_FACTORS);
    output.inside = min(tf.w, MAX_TESSELLATION_FACTORS);

    return output;
}

[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
PackedVaryingsToDS Hull(InputPatch<PackedVaryingsToDS, 3> input, uint id : SV_OutputControlPointID)
{
    // Pass-through
    return input[id];
}

[domain("tri")]
PackedVaryingsToPS Domain(TessellationFactors tessFactors, const OutputPatch<PackedVaryingsToDS, 3> input, float3 baryCoords : SV_DomainLocation)
{
    VaryingsToDS varying0 = UnpackVaryingsToDS(input[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(input[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(input[2]);

    VaryingsToDS varying = InterpolateWithBaryCoordsToDS(varying0, varying1, varying2, baryCoords);

    // We have Phong tessellation in all case where we don't have displacement only
#ifndef _TESSELLATION_DISPLACEMENT

    float3 p0 = varying0.vmesh.positionWS;
    float3 p1 = varying1.vmesh.positionWS;
    float3 p2 = varying2.vmesh.positionWS;

    float3 n0 = varying0.vmesh.normalWS;
    float3 n1 = varying1.vmesh.normalWS;
    float3 n2 = varying2.vmesh.normalWS;

    varying.vmesh.positionWS = PhongTessellation(   varying.vmesh.positionWS,
                                                    p0, p1, p2, n0, n1, n2,
                                                    baryCoords, _TessellationShapeFactor);
#endif

#if defined(_TESSELLATION_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT_PHONG)
    varying.vmesh.positionWS += GetTessellationDisplacement(varying.vmesh);
#endif

    return VertTesselation(varying);
}
