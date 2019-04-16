//forest-begin: sky occlusion

#define SKY_OCCLUSION 1
#if SKY_OCCLUSION

// Occlusion probes
sampler3D _OcclusionProbes;
float4x4 _OcclusionProbesWorldToLocal;
sampler3D _OcclusionProbesDetail;
float4x4 _OcclusionProbesWorldToLocalDetail;
float4 _AmbientProbeSH[7];

// Grass occlusion
sampler2D _GrassOcclusion;
float _GrassOcclusionAmountTerrain;
float _GrassOcclusionAmountGrass;
float _GrassOcclusionHeightFadeBottom;
float _GrassOcclusionHeightFadeTop;
float4x4 _GrassOcclusionWorldToLocal;
sampler2D _GrassOcclusionHeightmap;
float _GrassOcclusionHeightRange;
float _GrassOcclusionCullHeight;

float SampleGrassOcclusion(float2 terrainUV)
{
    return lerp(1.0, tex2D(_GrassOcclusion, terrainUV).a, _GrassOcclusionAmountTerrain);
}

float SampleGrassOcclusion(float3 positionWS)
{
    float3 pos = mul(_GrassOcclusionWorldToLocal, float4(positionWS, 1)).xyz;
    float terrainHeight = tex2D(_GrassOcclusionHeightmap, pos.xz).a;
    float height = pos.y - terrainHeight * _GrassOcclusionHeightRange;

    UNITY_BRANCH
    if(height < _GrassOcclusionCullHeight)
    {
        float xz = lerp(1.0, tex2D(_GrassOcclusion, pos.xz).a, _GrassOcclusionAmountGrass);
        return saturate(xz + smoothstep(_GrassOcclusionHeightFadeBottom, _GrassOcclusionHeightFadeTop, height));

        // alternatively:    
        // float amount = saturate(smoothstep(_GrassOcclusionHeightFade, 0, pos.y) * _GrassOcclusionAmount);
        // return lerp(1.0, tex2D(_GrassOcclusion, pos.xz).a, amount);
    }
    else
        return 1;
}

float SampleOcclusionProbes(float3 positionWS)
{
	// TODO: no full matrix mul needed, just scale and offset the pos (don't really need to support rotation)
    float occlusionProbes = 1;

    float3 pos = mul(_OcclusionProbesWorldToLocalDetail, float4(positionWS, 1)).xyz;

    UNITY_BRANCH
	if(all(pos > 0) && all(pos < 1))
    {
		occlusionProbes = tex3D(_OcclusionProbesDetail, pos).a;
	}
    else
    {
		pos = mul(_OcclusionProbesWorldToLocal, float4(positionWS, 1)).xyz;
		occlusionProbes = tex3D(_OcclusionProbes, pos).a;
	}

    return occlusionProbes;
}

float SampleSkyOcclusion(float3 positionRWS, out float grassOcclusion)
{
    float3 positionWS = GetAbsolutePositionWS(positionRWS);
    grassOcclusion = SampleGrassOcclusion(positionWS);
    return grassOcclusion * SampleOcclusionProbes(positionWS);
}

float SampleSkyOcclusion(float3 positionRWS, float2 terrainUV, out float grassOcclusion)
{
    float3 positionWS = GetAbsolutePositionWS(positionRWS);
    grassOcclusion = SampleGrassOcclusion(terrainUV);
    return grassOcclusion * SampleOcclusionProbes(positionWS);
}

#else
float SampleGrassOcclusion(float2 terrainUV) { return 1; }
float SampleSkyOcclusion(float3 positionRWS, out float grassOcclusion) { grassOcclusion = 1; return 1; }
float SampleSkyOcclusion(float3 positionRWS, float2 terrainUV, out float grassOcclusion) { grassOcclusion = 1; return 1; }
#endif
//forest-end:
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
