
void BuildFragInputsFromIntersection(IntersectionVertice currentVertex, RayIntersection rayIntersection, out FragInputs outFragInputs)
{
	outFragInputs.positionSS = float4(0.0, 0.0, 0.0, 0.0);
	outFragInputs.positionRWS = mul(ObjectToWorld3x4(), float4(currentVertex.positionOS, 1.0)).xyz - _WorldSpaceCameraPos;
	outFragInputs.texCoord0 = float4(currentVertex.texCoord0, 0.0, 0.0);
	outFragInputs.texCoord1 = float4(currentVertex.texCoord1, 0.0, 0.0);
	outFragInputs.texCoord2 = float4(currentVertex.texCoord2, 0.0, 0.0);
	outFragInputs.texCoord3 = float4(currentVertex.texCoord3, 0.0, 0.0);
	outFragInputs.color = currentVertex.vertexColor;

	// Let's compute the object space binormal
	float3 bitangent = cross(currentVertex.normalOS, currentVertex.tangentOS);
	float3x3 objectToWorld = (float3x3)WorldToObject3x4();
	outFragInputs.worldToTangent[0] = normalize(mul(currentVertex.tangentOS, objectToWorld));
	outFragInputs.worldToTangent[1] = normalize(mul(bitangent, objectToWorld));
	outFragInputs.worldToTangent[2] = normalize(mul(currentVertex.normalOS, objectToWorld));

	outFragInputs.isFrontFace = dot(rayIntersection.incidentDirection, outFragInputs.worldToTangent[2]) < 0.0f;
}
