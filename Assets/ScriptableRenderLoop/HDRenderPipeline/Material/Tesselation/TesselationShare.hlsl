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

// add [maxtessfactor(15)] ?
[UNITY_domain("tri")]
[UNITY_partitioning("fractional_odd")]
[UNITY_outputtopology("triangle_cw")]
[UNITY_patchconstantfunc("HullConstant")]
[UNITY_outputcontrolpoints(3)]
AttributesTesselation Hull(InputPatch<AttributesTesselation, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

[UNITY_domain("tri")]
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<AttributesTesselation, 3> input, float3 baryWeight : SV_DomainLocation)
{
    Attributes params = InterpolateWithBary(input[0], input[1], input[2], baryWeight);

    // perform displacement
    Displacement(params);

    // Evaluate regular vertex shader
    PackedVaryings outout = Vert(params);

    return outout;
}
