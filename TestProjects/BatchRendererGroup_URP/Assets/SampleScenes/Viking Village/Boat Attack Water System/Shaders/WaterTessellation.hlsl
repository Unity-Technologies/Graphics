#ifndef WATER_TESSELLATION_INCLUDED
#define WATER_TESSELLATION_INCLUDED

///////////////////////////////////////////////////////////////////////////////
//                  				Structs		                             //
///////////////////////////////////////////////////////////////////////////////

struct TessellationControlPoint
{
	float4 vertex : INTERNALTESSPOS;
	float4	texcoord 				: TEXCOORD0;	// Geometric UVs stored in xy, and world(pre-waves) in zw
	float3	posWS					: TEXCOORD1;	// world position of the vertices
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HS_ConstantOutput
{
	float TessFactor[3]    : SV_TessFactor;
	float InsideTessFactor : SV_InsideTessFactor;
};

///////////////////////////////////////////////////////////////////////////////
//                         Tessellation functions                            //
///////////////////////////////////////////////////////////////////////////////

half _TessellationEdgeLength;

float TessellationEdgeFactor (float3 p0, float3 p1) 
{
    float edgeLength = distance(p0, p1);

    float3 edgeCenter = (p0 + p1) * 0.5;
    float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

    return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * viewDistance);
}

TessellationControlPoint TessellationVertex( WaterVertexInput v )
{
    TessellationControlPoint o;
    o.vertex = v.vertex;
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.texcoord.xy = v.texcoord;
    o.texcoord.zw = o.posWS.xz;
    //o.color = v.color;
    return o;
}

HS_ConstantOutput HSConstant( InputPatch<TessellationControlPoint, 3> Input )
{
    float3 p0 = TransformObjectToWorld(Input[0].vertex.xyz);
    float3 p1 = TransformObjectToWorld(Input[1].vertex.xyz);
    float3 p2 = TransformObjectToWorld(Input[2].vertex.xyz);
    HS_ConstantOutput o = (HS_ConstantOutput)0;
    o.TessFactor[0] = TessellationEdgeFactor(p1, p2);
    o.TessFactor[1] = TessellationEdgeFactor(p2, p0);
    o.TessFactor[2] = TessellationEdgeFactor(p0, p1);
    o.InsideTessFactor = (TessellationEdgeFactor(p1, p2) + 
                                TessellationEdgeFactor(p2, p0) + 
                                TessellationEdgeFactor(p0, p1)) * (1 / 3.0);
    return o;
}

[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HSConstant")]
[outputcontrolpoints(3)]
TessellationControlPoint Hull( InputPatch<TessellationControlPoint, 3> Input, uint uCPID : SV_OutputControlPointID )
{
    return Input[uCPID];
}



// Domain: replaces vert for tessellation version
[domain("tri")]
WaterVertexOutput Domain( HS_ConstantOutput HSConstantData, const OutputPatch<TessellationControlPoint, 3> Input, float3 BarycentricCoords : SV_DomainLocation)
{
    WaterVertexOutput o = (WaterVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	/////////////////////Tessellation////////////////////////
	float fU = BarycentricCoords.x;
	float fV = BarycentricCoords.y;
	float fW = BarycentricCoords.z;

	float4 vertex = Input[0].vertex * fU + Input[1].vertex * fV + Input[2].vertex * fW;
	o.uv = Input[0].texcoord * fU + Input[1].texcoord * fV + Input[2].texcoord * fW;
	o.posWS = Input[0].posWS * fU + Input[1].posWS * fV + Input[2].posWS * fW;

    o = WaveVertexOperations(o);

    return o;
}

#endif // WATER_TESSELLATION_INCLUDED