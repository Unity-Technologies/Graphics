#ifndef UNITY_RAYTRACING_INTERSECTION_INCLUDED
#define UNITY_RAYTRACING_INTERSECTION_INCLUDED

// Engine includes
#include "UnityRaytracingMeshUtils.cginc"

// Raycone structure that defines the stateof the ray
struct RayCone
{
	float width;
	float spreadAngle;
};

// Structure that defines the current state of the intersection
struct RayIntersection
{
	// Origin of the current ray -- FIXME: can be obtained by WorldRayPosition(), should we remove it?
	float3  origin;
	// Direction of the current ray -- FIXME: can be obtained by WorldRayDirection(), should we remove it?
	float3  incidentDirection;
	// Distance of the intersection
	float t;
	// Value that holds the color of the ray
	float3 color;
	// Cone representation of the ray
	RayCone cone;
	// The remaining available depth for the current Ray
	uint remainingDepth;
	// Current sample index
	uint sampleIndex;
	// Ray counter (used for multibounce)
	uint rayCount;
	// Pixel coordinate from which the initial ray was launched
	uint2 pixelCoord;
	// Velocity for the intersection point
	float velocity;
};

struct AttributeData
{
	// Barycentric value of the intersection
	float2 barycentrics;
};

// Macro that interpolate any attribute using barycentric coordinates
#define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

// Structure to fill for intersections
struct IntersectionVertex
{
	// Object space normal of the vertex
	float3 normalOS;
	// Object space tangent of the vertex
	float4 tangentOS;
	// UV coordinates
	float4 texCoord0;
	float4 texCoord1;
	float4 texCoord2;
	float4 texCoord3;
	float4 color;

#ifdef USE_RAY_CONE_LOD
	// Value used for LOD sampling
	float  triangleArea;
	float  texCoord0Area;
	float  texCoord1Area;
	float  texCoord2Area;
	float  texCoord3Area;
#endif
};

// Fetch the intersetion vertex data for the target vertex
void FetchIntersectionVertex(uint vertexIndex, out IntersectionVertex outVertex)
{
    outVertex.normalOS   = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);

    #ifdef ATTRIBUTES_NEED_TANGENT
    outVertex.tangentOS  = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTangent);
    #else
    outVertex.tangentOS  = 0.0;
    #endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD0
    outVertex.texCoord0  = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTexCoord0);
    #else
    outVertex.texCoord0  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD1
    outVertex.texCoord1  = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTexCoord1);
    #else
    outVertex.texCoord1  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD2
    outVertex.texCoord2  = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTexCoord2);
	#else
	outVertex.texCoord2  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD3
    outVertex.texCoord3  = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTexCoord3);
	#else
	outVertex.texCoord3  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_COLOR
    outVertex.color      = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeColor);
	#else
	outVertex.color  = 0.0;
	#endif
}

void GetCurrentIntersectionVertex(AttributeData attributeData, out IntersectionVertex outVertex)
{
	// Fetch the indices of the currentr triangle
	uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());

	// Fetch the 3 vertices
	IntersectionVertex v0, v1, v2;
	FetchIntersectionVertex(triangleIndices.x, v0);
	FetchIntersectionVertex(triangleIndices.y, v1);
	FetchIntersectionVertex(triangleIndices.z, v2);

	// Compute the full barycentric coordinates
	float3 barycentricCoordinates = float3(1.0 - attributeData.barycentrics.x - attributeData.barycentrics.y, attributeData.barycentrics.x, attributeData.barycentrics.y);

	// Interpolate all the data
	outVertex.normalOS   = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normalOS, v1.normalOS, v2.normalOS, barycentricCoordinates);
    
    #ifdef ATTRIBUTES_NEED_TANGENT
	outVertex.tangentOS  = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.tangentOS, v1.tangentOS, v2.tangentOS, barycentricCoordinates);
	#else
	outVertex.tangentOS  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD0
	outVertex.texCoord0  = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord0, v1.texCoord0, v2.texCoord0, barycentricCoordinates);
	#else
	outVertex.texCoord0  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD1
	outVertex.texCoord1  = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord1, v1.texCoord1, v2.texCoord1, barycentricCoordinates);
	#else
	outVertex.texCoord1  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD2
	outVertex.texCoord2  = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord2, v1.texCoord2, v2.texCoord2, barycentricCoordinates);
	#else
	outVertex.texCoord2  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_TEXCOORD3
	outVertex.texCoord3  = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord3, v1.texCoord3, v2.texCoord3, barycentricCoordinates);
	#else
	outVertex.texCoord3  = 0.0;
	#endif

	#ifdef ATTRIBUTES_NEED_COLOR
	outVertex.color      = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.color, v1.color, v2.color, barycentricCoordinates);
	#else
	outVertex.color      = 0.0;
	#endif

#ifdef USE_RAY_CONE_LOD
	// Compute the lambda value (area computed in object space)
	outVertex.triangleArea  = length(cross(v1.positionOS - v0.positionOS, v2.positionOS - v0.positionOS));
	outVertex.texCoord0Area = abs((v1.texCoord0.x - v0.texCoord0.x) * (v2.texCoord0.y - v0.texCoord0.y) - (v2.texCoord0.x - v0.texCoord0.x) * (v1.texCoord0.y - v0.texCoord0.y));
	outVertex.texCoord1Area = abs((v1.texCoord1.x - v0.texCoord1.x) * (v2.texCoord1.y - v0.texCoord1.y) - (v2.texCoord1.x - v0.texCoord1.x) * (v1.texCoord1.y - v0.texCoord1.y));
	outVertex.texCoord2Area = abs((v1.texCoord2.x - v0.texCoord2.x) * (v2.texCoord2.y - v0.texCoord2.y) - (v2.texCoord2.x - v0.texCoord2.x) * (v1.texCoord2.y - v0.texCoord2.y));
	outVertex.texCoord3Area = abs((v1.texCoord3.x - v0.texCoord3.x) * (v2.texCoord3.y - v0.texCoord3.y) - (v2.texCoord3.x - v0.texCoord3.x) * (v1.texCoord3.y - v0.texCoord3.y));
#endif
}

#endif // UNITY_RAYTRACING_INTERSECTION_INCLUDED
