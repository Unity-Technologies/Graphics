AttributesTesselation VertTesselation(Attributes input)
{
    return AttributesToAttributesTesselation(input);
}

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors HullConstant(InputPatch<AttributesTesselation, 3> input)
{
    Attributes params[3];
    params[0] = AttributesTesselationToAttributes(input[0]);
    params[1] = AttributesTesselationToAttributes(input[1]);
    params[2] = AttributesTesselationToAttributes(input[2]);
 
    float4 tf = TesselationEdge(params[0], params[1], params[2]);

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
AttributesTesselation Hull(InputPatch<AttributesTesselation, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

[domain("tri")]
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<AttributesTesselation, 3> input, float3 baryWeight : SV_DomainLocation)
{
    Attributes params = InterpolateWithBary(input[0], input[1], input[2], baryWeight);

    // perform displacement
    Displacement(params);

    // Evaluate regular vertex shader
    PackedVaryings outout = Vert(params);

    return outout;
}
