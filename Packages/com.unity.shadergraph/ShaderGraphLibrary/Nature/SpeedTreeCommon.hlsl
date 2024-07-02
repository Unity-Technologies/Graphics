// Unity built-in shader source. Copyright (c) 2023 Unity Technologies. MIT license (see license.txt)

#ifndef SPEEDTREE_COMMON_INCLUDED
#define SPEEDTREE_COMMON_INCLUDED


float3 DoLeafFacing(float3 vPos, float3 anchor)
{
    float3 facingPosition = vPos - anchor; // move to origin
    float offsetLen = length(facingPosition);

    // rotate X -90deg: normals keep looking 'up' while cards/leaves now 'stand up' and face the view plane
    facingPosition = float3(facingPosition.x, -facingPosition.z, facingPosition.y);

    // extract scale from model matrix
    float3x3 modelMatrix = (float3x3) GetObjectToWorldMatrix(); // UNITY_MATRIX_M
    float3 scale = float3(
        length(float3(modelMatrix[0][0], modelMatrix[1][0], modelMatrix[2][0])),
        length(float3(modelMatrix[0][1], modelMatrix[1][1], modelMatrix[2][1])),
        length(float3(modelMatrix[0][2], modelMatrix[1][2], modelMatrix[2][2]))
    );
    
    // inverse of model : discards object rotations & scale
    // inverse of view  : discards camera rotations
    float3x3 modelMatrixInv = (float3x3) GetWorldToObjectMatrix(); // UNITY_MATRIX_I_M
    float3x3 viewMatrixInv  = (float3x3) GetViewToWorldMatrix();   // UNITY_MATRIX_I_V
    float3x3 matCardFacingTransform = mul(modelMatrixInv, viewMatrixInv);
    
    // re-encode the scale into the final transformation (otherwise cards would look small if tree is scaled up via world transform)
    matCardFacingTransform[0] *= scale.x; 
    matCardFacingTransform[1] *= scale.y;
    matCardFacingTransform[2] *= scale.z;

    // make the leaves/cards face the camera
    facingPosition = mul(matCardFacingTransform, facingPosition.xyz);
    facingPosition = normalize(facingPosition) * offsetLen; // make sure the offset vector is still scaled
    
    return facingPosition + anchor; // move back to branch
}

#define SPEEDTREE_SUPPORT_NON_UNIFORM_SCALING 0
float3 TransformWindVectorFromWorldToLocalSpace(float3 vWindDirection)
{
    // we intend to transform the world-space wind vector into local space.
    float3x3 modelMatrixInv = (float3x3) GetWorldToObjectMatrix(); // UNITY_MATRIX_I_M
#if SPEEDTREE_SUPPORT_NON_UNIFORM_SCALING 
    // the inverse world matrix would contain scale transformation as well, so we need
    // to get rid of scaling of the wind direction while doing inverse rotation.
    float3x3 modelMatrix    = (float3x3) GetObjectToWorldMatrix(); // UNITY_MATRIX_M
    float3 scaleInv = float3(
        length(float3(modelMatrix[0][0], modelMatrix[1][0], modelMatrix[2][0])),
        length(float3(modelMatrix[0][1], modelMatrix[1][1], modelMatrix[2][1])),
        length(float3(modelMatrix[0][2], modelMatrix[1][2], modelMatrix[2][2]))
    );
    float3x3 matWorldToLocalSpaceRotation = float3x3( // 3x3 discards translation
        modelMatrixInv[0][0] * scaleInv.x, modelMatrixInv[0][1]             , modelMatrixInv[0][2],
        modelMatrixInv[1][0]             , modelMatrixInv[1][1] * scaleInv.y, modelMatrixInv[1][2],
        modelMatrixInv[2][0]             , modelMatrixInv[2][1]             , modelMatrixInv[2][2] * scaleInv.z
    );
    float3 vLocalSpaceWind = mul(matWorldToLocalSpaceRotation, vWindDirection);
#else
    // Assume uniform scaling for the object -- discard translation and invert object rotations (and scale).
    // We'll normalize to get rid of scaling after the transformation.
    float3 vLocalSpaceWind = mul(modelMatrixInv, vWindDirection);
#endif
    float windVecLength = length(vLocalSpaceWind);
    if (windVecLength > 1e-5)
        vLocalSpaceWind *= (1.0f / windVecLength); // normalize
    return vLocalSpaceWind;
}

#define ST_GEOM_TYPE_BRANCH     0
#define ST_GEOM_TYPE_FROND      1
#define ST_GEOM_TYPE_LEAF       2
#define ST_GEOM_TYPE_FACINGLEAF 3
int GetGeometryType(float4 uv3, out bool bLeafTwo)
{
    int geometryType = (int) (uv3.w + 0.25);
    bLeafTwo = geometryType > ST_GEOM_TYPE_FACINGLEAF;
    if (bLeafTwo)
    {
        geometryType -= 2;
    }
    return geometryType;
}

// shadergraph stubs
void SpeedTree8LeafFacing_float(float3 vVertexLocalPosition, float4 UV1, float4 UV2, float4 UV3, out float3 vVertexLocalPositionOut)
{
    vVertexLocalPositionOut = vVertexLocalPosition;
    bool bDummy = false;
    if (GetGeometryType(UV3, bDummy) == ST_GEOM_TYPE_FACINGLEAF)
    {
        float3 vAnchorPosition = float3(UV1.zw, UV2.w);
        vVertexLocalPositionOut = DoLeafFacing(vVertexLocalPosition, vAnchorPosition);
    }
}
void SpeedTree9LeafFacing_float(float3 vVertexLocalPosition, float4 UV2, float4 UV3, out float3 vVertexLocalPositionOut)
{
    vVertexLocalPositionOut = vVertexLocalPosition;
    const bool bHasCameraFacingLeaf = UV3.w > 0.0f || UV2.w > 0.0f;
    if (bHasCameraFacingLeaf)
    {
        const float3 vAnchorPosition = UV3.w > 0.0f ? UV3.xyz : UV2.xyz;
        vVertexLocalPositionOut = DoLeafFacing(vVertexLocalPosition, vAnchorPosition);
    }
}

void SpeedTreeLODTransition_float(float3 ObjectSpacePosition, float4 ObjectSpacePositionNextLOD, const bool bBillboard, out float3 OutObjectSpacePosition)
{
    OutObjectSpacePosition = bBillboard
        ? ObjectSpacePosition
        : lerp(ObjectSpacePosition, ObjectSpacePositionNextLOD.xyz, unity_LODFade.x);
}

#endif // SPEEDTREE_COMMON_INCLUDED
