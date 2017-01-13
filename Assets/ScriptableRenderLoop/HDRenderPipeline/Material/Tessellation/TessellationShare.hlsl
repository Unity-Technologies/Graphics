struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors HullConstant(InputPatch<PackedVaryingsDS, 3> input)
{ 
    VaryingsDS varyingDS0 = UnpackVaryingsDS(input[0]);
    VaryingsDS varyingDS1 = UnpackVaryingsDS(input[1]);
    VaryingsDS varyingDS2 = UnpackVaryingsDS(input[2]);

    float4 tf = TessellationEdge(   varyingDS0.positionWS, varyingDS1.positionWS, varyingDS2.positionWS, 
                                    varyingDS0.normalWS, varyingDS1.normalWS, varyingDS2.normalWS);

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
PackedVaryingsDS Hull(InputPatch<PackedVaryingsDS, 3> input, uint id : SV_OutputControlPointID)
{
    // Pass-through
    return input[id];
}

[domain("tri")]
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<PackedVaryingsDS, 3> input, float3 baryCoords : SV_DomainLocation)
{
    VaryingsDS varyingDS0 = UnpackVaryingsDS(input[0]);
    VaryingsDS varyingDS1 = UnpackVaryingsDS(input[1]);
    VaryingsDS varyingDS2 = UnpackVaryingsDS(input[2]);

    VaryingsDS varyingDS = InterpolateWithBaryCoords(varyingDS0, varyingDS1, varyingDS2, baryCoords);

    // We have Phong tessellation in all case where we don't have displacement only
#ifndef _TESSELLATION_DISPLACEMENT
    varyingDS.positionWS = PhongTessellation(   varyingDS.positionWS,
                                                varyingDS0.positionWS, varyingDS1.positionWS, varyingDS2.positionWS,
                                                varyingDS0.normalWS, varyingDS1.normalWS, varyingDS2.normalWS,
                                                baryCoords, _TessellationShapeFactor);
#endif

#if defined(_TESSELLATION_DISPLACEMENT) || defined(_TESSELLATION_DISPLACEMENT_PHONG)
    varyingDS.positionWS = GetDisplacement(varyingDS);
#endif

    // Evaluate part of the vertex shader not done
    PackedVaryings outout = VertTesselation(varyingDS);

    return outout;
}
