AttributesTessellation VertTessellation(Attributes input)
{
    return AttributesToAttributesTessellation(input);
}

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors HullConstant(InputPatch<AttributesTessellation, 3> input)
{
    Attributes params[3];
    params[0] = AttributesTessellationToAttributes(input[0]);
    params[1] = AttributesTessellationToAttributes(input[1]);
    params[2] = AttributesTessellationToAttributes(input[2]);
 
#if (SHADERPASS != SHADERPASS_VELOCITY) && (SHADERPASS != SHADERPASS_DISTORTION)

    // TEMP: We will provide world position but for now convert to world position here
    float3 p0 = TransformObjectToWorld(input[0].positionOS);
    float3 n0 = TransformObjectToWorldNormal(input[0].normalOS);

    float3 p1 = TransformObjectToWorld(input[1].positionOS);
    float3 n1 = TransformObjectToWorldNormal(input[1].normalOS);

    float3 p2 = TransformObjectToWorld(input[2].positionOS);
    float3 n2 = TransformObjectToWorldNormal(input[2].normalOS);

    float4 tf = TessellationEdge(p0, p1, p2, n0, n1, n2);
#else
    float4 tf = float4(0.0, 0.0, 0.0, 0.0);
#endif

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
AttributesTessellation Hull(InputPatch<AttributesTessellation, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

[domain("tri")]
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<AttributesTessellation, 3> input, float3 baryCoords : SV_DomainLocation)
{
    Attributes params = InterpolateWithBaryCoords(input[0], input[1], input[2], baryCoords);

#ifndef _TESSELLATION_DISPLACEMENT // We have Phong tessellation in all case where we don't have only displacement
#if (SHADERPASS != SHADERPASS_VELOCITY) && (SHADERPASS != SHADERPASS_DISTORTION)
    params.positionOS = PhongTessellation(  params.positionOS,
                                            input[0].positionOS, input[1].positionOS, input[2].positionOS,
                                            input[0].normalOS, input[1].normalOS, input[2].normalOS,
                                            baryCoords, _TessellationShapeFactor);
#endif
#endif

    // perform displacement
    Displacement(params);

    // Evaluate regular vertex shader
    PackedVaryings outout = Vert(params);

    return outout;
}
