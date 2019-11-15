// FIXME: Should probably be renamed as we don't need rayIntersection as input anymore (neither do we need incidentDirection)
void BuildFragInputsFromIntersection(IntersectionVertex currentVertex, float3 incidentDirection, out FragInputs outFragInputs)
{
	outFragInputs.positionSS = float4(0.0, 0.0, 0.0, 0.0);
	outFragInputs.positionRWS = mul(ObjectToWorld3x4(), float4(currentVertex.positionOS, 1.0)).xyz - _WorldSpaceCameraPos;
	outFragInputs.texCoord0 = float4(currentVertex.texCoord0, 0.0, 0.0);
	outFragInputs.texCoord1 = float4(currentVertex.texCoord1, 0.0, 0.0);
	outFragInputs.texCoord2 = float4(currentVertex.texCoord2, 0.0, 0.0);
	outFragInputs.texCoord3 = float4(currentVertex.texCoord3, 0.0, 0.0);
	outFragInputs.color = currentVertex.color;

    float3 normalWS = normalize(mul(currentVertex.normalOS, (float3x3)WorldToObject3x4()));
	float4 tangentWS = float4(normalize(mul(currentVertex.tangentOS.xyz, (float3x3)WorldToObject3x4())), currentVertex.tangentOS.w);
	outFragInputs.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);

	outFragInputs.isFrontFace = dot(incidentDirection, outFragInputs.tangentToWorld[2]) < 0.0f;
}
