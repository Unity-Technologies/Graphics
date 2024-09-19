// Unity built-in shader source. Copyright (c) 2023 Unity Technologies. MIT license (see license.txt)

#ifndef SPEEDTREE_COMMON_INCLUDED
#define SPEEDTREE_COMMON_INCLUDED

#define ST_GEOM_TYPE_BRANCH     0
#define ST_GEOM_TYPE_FROND      1
#define ST_GEOM_TYPE_LEAF       2
#define ST_GEOM_TYPE_FACINGLEAF 3

// @uv2 packs the object space position of the next LOD
float3 ApplySmoothLODTransition(float3 ObjectSpacePosition, float3 uv2)
{
    return lerp(ObjectSpacePosition, uv2, unity_LODFade.x);
}

float3 DoLeafFacing(float3 vPos, float3 anchor)
{
    float3 facingPosition = vPos - anchor;

    // face camera-facing leaf to camera
    float offsetLen = length(facingPosition);
    facingPosition = float3(facingPosition.x, -facingPosition.z, facingPosition.y);
    float4x4 itmv = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
    facingPosition = mul(facingPosition.xyz, (float3x3)itmv);
    facingPosition = normalize(facingPosition) * offsetLen; // make sure the offset vector is still scaled

    return facingPosition + anchor;
}

int GetGeometryType(float4 uv3, out bool bLeafTwo)
{
    int geometryType = (int)(uv3.w + 0.25);
    bLeafTwo = geometryType > ST_GEOM_TYPE_FACINGLEAF;
    if (bLeafTwo)
    {
        geometryType -= 2;
    }
    return geometryType;
}

#endif // SPEEDTREE_COMMON_INCLUDED
