//forest-begin: Tree occlusion

//UnityPerMaterial
//float _UseTreeOcclusion;
//float _TreeAO;
//float _TreeAOBias;
//float _TreeAO2;
//float _TreeAOBias2;
//float _TreeDO;
//float _TreeDOBias;
//float _TreeDO2;
//float _TreeDOBias2;
//float _Tree12Width;

// Freeload of an already passed global sun vector
float3 _AtmosphericScatteringSunVector;

float GetTreeOcclusion(float3 positionRWS, float4 treeOcclusionInput) {
#if defined(_ANIM_SINGLE_PIVOT_COLOR) || defined(_ANIM_HIERARCHY_PIVOT)
	if(_UseTreeOcclusion) {
		float3 positionWS = GetAbsolutePositionWS(positionRWS);
		float treeWidth = _Tree12Width == 0 ? 1.f : saturate((positionWS.y - UNITY_MATRIX_M._m13) / _Tree12Width);
		float treeDO = lerp(_TreeDO, _TreeDO2, treeWidth);
		float treeAO = lerp(_TreeAO, _TreeAO2, treeWidth);
		float4 lightDir = float4(-_AtmosphericScatteringSunVector * treeDO, treeAO);
		float treeDOBias = lerp(_TreeDOBias, _TreeDOBias2, treeWidth);
		float treeAOBias = lerp(_TreeAOBias, _TreeAOBias2, treeWidth);
		return saturate(dot(saturate(treeOcclusionInput + float4(treeDOBias.rrr, treeAOBias)), lightDir));
	}
	else
#endif
	{
		return 1.f;
	}
}
//forest-end:

// Flipping or mirroring a normal can be done directly on the tangent space. This has the benefit to apply to the whole process either in surface gradient or not.
// This function will modify FragInputs and this is not propagate outside of GetSurfaceAndBuiltinData(). This is ok as tangent space is not use outside of GetSurfaceAndBuiltinData().
void ApplyDoubleSidedFlipOrMirror(inout FragInputs input, float3 doubleSidedConstants)
{
#ifdef _DOUBLESIDED_ON
    // 'doubleSidedConstants' is float3(-1, -1, -1) in flip mode and float3(1, 1, -1) in mirror mode.
    // It's float3(1, 1, 1) in the none mode.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.z;
    // For the 'Flip' mode, we should not modify the tangent and the bitangent (which correspond
    // to the surface derivatives), and instead modify (invert) the displacements.
    input.tangentToWorld[2] = flipSign * input.tangentToWorld[2]; // normal
#endif
}

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalWS(FragInputs input, float3 normalTS, out float3 normalWS, float3 doubleSidedConstants)
{
#ifdef SURFACE_GRADIENT

#ifdef _DOUBLESIDED_ON
    // Flip the displacements (the entire surface gradient) in the 'flip normal' mode.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    normalTS *= flipSign;
#endif

    normalWS = SurfaceGradientResolveNormal(input.tangentToWorld[2], normalTS);

#else // SURFACE_GRADIENT

#ifdef _DOUBLESIDED_ON
    // Just flip the TB in the 'flip normal' mode. Conceptually wrong, but it works.
    float flipSign = input.isFrontFace ? 1.0 : doubleSidedConstants.x;
    input.tangentToWorld[0] = flipSign * input.tangentToWorld[0]; // tangent
    input.tangentToWorld[1] = flipSign * input.tangentToWorld[1]; // bitangent
#endif // _DOUBLESIDED_ON

    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.tangentToWorld));

#endif // SURFACE_GRADIENT
}
