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

float3 ProjectPointOnPlane(float3 position, float3 planePosition, float3 planeNormal)
{
    return position - (dot(position - planePosition, planeNormal) * planeNormal);
}

// p0, p1, p2 triangle world position
// p0, p1, p2 triangle world vertex normal
float3 PhongTessellation(float3 positionWS, float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2, float3 baryCoords, float shape)
{
    float3 c0 = ProjectPointOnPlane(positionWS, p0, n0);
    float3 c1 = ProjectPointOnPlane(positionWS, p1, n1);
    float3 c2 = ProjectPointOnPlane(positionWS, p2, n2);

    float3 phongPositionWS = baryCoords.x * c0 + baryCoords.y * c1 + baryCoords.z * c2;

    return lerp(positionWS, phongPositionWS, shape);
}

[domain("tri")]
PackedVaryings Domain(TessellationFactors tessFactors, const OutputPatch<AttributesTesselation, 3> input, float3 baryCoords : SV_DomainLocation)
{
    Attributes params = InterpolateWithBaryCoords(input[0], input[1], input[2], baryCoords);

#ifndef _TESSELATION_DISPLACEMENT // We have Phong tesselation in all case where we don't have only displacement
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
