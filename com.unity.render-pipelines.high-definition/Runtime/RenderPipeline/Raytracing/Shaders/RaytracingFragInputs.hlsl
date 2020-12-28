// FIXME: Should probably be renamed as we don't need rayIntersection as input anymore (neither do we need incidentDirection)
void BuildFragInputsFromIntersection(IntersectionVertex currentVertex, float3 incidentDirection, out FragInputs outFragInputs)
{
	outFragInputs.positionSS = float4(0.0, 0.0, 0.0, 0.0);
	outFragInputs.positionRWS = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();
	outFragInputs.texCoord0 = currentVertex.texCoord0;
	outFragInputs.texCoord1 = currentVertex.texCoord1;
	outFragInputs.texCoord2 = currentVertex.texCoord2;
	outFragInputs.texCoord3 = currentVertex.texCoord3;
	outFragInputs.color = currentVertex.color;

    float3 normalWS = normalize(mul(currentVertex.normalOS, (float3x3)WorldToObject3x4()));
	float3 tangentWS = normalize(mul(currentVertex.tangentOS.xyz, (float3x3)WorldToObject3x4()));
	outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, sign(currentVertex.tangentOS.w));

	outFragInputs.isFrontFace = dot(incidentDirection, outFragInputs.tangentToWorld[2]) < 0.0f;
}