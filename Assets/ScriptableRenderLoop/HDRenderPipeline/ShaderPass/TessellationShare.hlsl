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

    float4 tf = TessellationEdge(   varying0.vmesh.positionWS, varying1.vmesh.positionWS, varying2.vmesh.positionWS,
                                    varying0.vmesh.normalWS, varying1.vmesh.normalWS, varying2.vmesh.normalWS);

    TessellationFactors ouput;
    ouput.edge[0] = tf.x;
    ouput.edge[1] = tf.y;
    ouput.edge[2] = tf.z;
    ouput.inside = tf.w;

    return ouput;
}

[maxtessfactor(15.0)] // AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
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
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<PackedVaryingsToDS, 3> input, float3 baryCoords : SV_DomainLocation)
{
    VaryingsToDS varying0 = UnpackVaryingsToDS(input[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(input[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(input[2]);

    VaryingsToDS varying = InterpolateWithBaryCoords(varying0, varying1, varying2, baryCoords);

    // We have Phong tessellation in all case where we don't have displacement only
#ifndef _TESSELLATION_DISPLACEMENT
    varying.vmesh.positionWS = PhongTessellation(   varying.positionWS,
                                                    varying0.vmesh.positionWS, varying1.vmesh.positionWS, varying2.vmesh.positionWS,
                                                    varying0.vmesh.normalWS, varying1.vmesh.normalWS, varying2.vmesh.normalWS,
                                                    baryCoords, _TessellationShapeFactor);
#endif

#if defined(_TESSELLATION_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT_PHONG)
    varying.vmesh.positionWS = GetDisplacement(varying.vmesh);
#endif

    return VertTesselation(varying);
}
