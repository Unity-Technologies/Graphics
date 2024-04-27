#ifndef CAPSULE_SHADOWS_TILING_HLSL
#define CAPSULE_SHADOWS_TILING_HLSL

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleShadowsCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluderData.hlsl"

#define CAPSULE_SHADOW_TILE_SIZE                8
#define CAPSULE_SHADOW_DEPTH_RANGE_COUNT        2

#define CAPSULE_SHADOW_COARSE_TILE_SHADOW_COUNT(TILE_INDEX)     (CAPSULESHADOWCOUNTERSLOT_COARSE_TILE_SHADOW_COUNT_BASE + coarseTileIndex)
#define CAPSULE_SHADOW_COARSE_TILE_DEPTH_RANGE_BASE(TILE_INDEX) (CAPSULESHADOWCOUNTERSLOT_COARSE_TILE_DEPTH_RANGE_BASE + 2*coarseTileIndex)

#define CAPSULE_SHADOW_GLOBAL_INDEX(COARSE_TILE_INDEX, SHADOW_INDEX)    ((COARSE_TILE_INDEX)*_CapsuleOccluderCount*_CapsuleCasterCount + (SHADOW_INDEX))

uint CoarseTileIndex_X(uint2 tileCoord)
{
    return (unity_StereoEyeIndex*_CapsuleRenderSizeInCoarseTilesY + tileCoord.y)*_CapsuleRenderSizeInCoarseTilesX + tileCoord.x;
}

float3 ViewspaceFromXY(float2 posVS_XY, float linearDepth)
{
#if USE_LEFT_HAND_CAMERA_SPACE
    float viewZ = -linearDepth;
#else
    float viewZ = linearDepth;
#endif
    return float3(posVS_XY * abs(linearDepth), -viewZ);
}

// cached per depth range per caster, used to cull capsules that would not affect the tile
struct CasterCullingData
{
    float3 tileToLightDir;
    float tileToLightDistance;
    float penumbraRcpSinTheta;
    float penumbraTanTheta;
};

// do not call for CAPSULESHADOWCASTERTYPE_INDIRECT, only valid for direct shadow casters
bool CapsuleShadowCasterIntersectsTile(
    CapsuleShadowCaster caster,
    float3 tileCenterWS,
    float tileRadiusWS,
    out CasterCullingData cullingData)
{
    uint casterType = GetCasterType(caster);
    bool isValid;
    float penumbraCosTheta;
    if (casterType == CAPSULESHADOWCASTERTYPE_DIRECTIONAL)
    {
        cullingData.tileToLightDir = caster.directionWS;
        cullingData.tileToLightDistance = FLT_MAX;

        penumbraCosTheta = caster.maxCosTheta;

        // TODO: are there any ways to cull the tile outside of the directional light volume?
        isValid = true;
    }
    else // CAPSULESHADOWCASTERTYPE_POINT or CAPSULESHADOWCASTERTYPE_SPOT
    {
        float3 tileToLightVec = caster.positionRWS - tileCenterWS;
        float tileToLightDistance = length(tileToLightVec);;
        cullingData.tileToLightDir = tileToLightVec/tileToLightDistance;
        cullingData.tileToLightDistance = tileToLightDistance;

        float sinTheta = max(0.f, caster.radiusWS/(tileToLightDistance - tileRadiusWS));
        penumbraCosTheta = min(caster.maxCosTheta, MatchingSinCos(sinTheta));
                
        // check the tile is within the volume of the light
        isValid = (tileToLightDistance < caster.lightRange + tileRadiusWS);
        if (casterType == CAPSULESHADOWCASTERTYPE_SPOT)
        {
            float tileConeSinTheta = tileRadiusWS/tileToLightDistance;
            float tileConeCosTheta = MatchingSinCos(tileConeSinTheta);

            float spotConeCosTheta = caster.spotCosTheta;
            float spotConeSinTheta = MatchingSinCos(spotConeCosTheta);

            // cos(a + b) = cos(a)cos(b) - sin(a)sin(b)
            float combinedCosTheta = spotConeCosTheta*tileConeCosTheta - spotConeSinTheta*tileConeSinTheta;

            // check the tile is within the spot cone
            float cosAngleBetweenLightDirAndTileCenter = dot(cullingData.tileToLightDir, caster.directionWS);
            isValid = isValid && (cosAngleBetweenLightDirAndTileCenter > combinedCosTheta);
        }
    }

    float penumbraSinTheta = MatchingSinCos(penumbraCosTheta);
    cullingData.penumbraRcpSinTheta = 1.f/penumbraSinTheta;
    cullingData.penumbraTanTheta = penumbraSinTheta/penumbraCosTheta;

    return isValid;
}

// do not call for CAPSULESHADOWCASTERTYPE_INDIRECT, only valid for direct shadow casters
bool CapsuleShadowVolumeIntersectsTile(
    float3 tileToCapsuleVec,
    float tileRadiusWS,
    float capsuleSignedDistanceFromTile,
    float3 capsuleAxisDirWS,
    float capsuleOffset,
    float capsuleRadius,
    CasterCullingData cullingData,
    float shadowRange)
{
    // check range
    bool intersectsRange = (capsuleSignedDistanceFromTile < shadowRange);

    // check the smallest angle between the light axis and the capsule, from points in the tile
    bool intersectsShadow = (capsuleSignedDistanceFromTile < 0.f);
    if (capsuleSignedDistanceFromTile < cullingData.tileToLightDistance)
    {
        float closestT = clamp(
            RayVsRayClosestPoints(tileToCapsuleVec, capsuleAxisDirWS, 0.f, cullingData.tileToLightDir).x,
            -capsuleOffset, capsuleOffset);
        float shiftRadius = capsuleRadius + tileRadiusWS;
                    
        // check closest point
        {
            float3 tileToCheckPos = tileToCapsuleVec + capsuleAxisDirWS*closestT;

            float distanceAlongAxis = dot(cullingData.tileToLightDir, tileToCheckPos);
            float distanceFromAxis = length(tileToCheckPos - distanceAlongAxis*cullingData.tileToLightDir);
            distanceAlongAxis += shiftRadius*cullingData.penumbraRcpSinTheta;

            intersectsShadow = intersectsShadow || (distanceFromAxis < distanceAlongAxis*cullingData.penumbraTanTheta);
        }
        // check other end
        if (0.f != closestT && abs(closestT) == capsuleOffset)
        {
            float3 tileToCheckPos = tileToCapsuleVec - capsuleAxisDirWS*closestT;

            float distanceAlongAxis = dot(cullingData.tileToLightDir, tileToCheckPos);
            float distanceFromAxis = length(tileToCheckPos - distanceAlongAxis*cullingData.tileToLightDir);
            distanceAlongAxis += shiftRadius*cullingData.penumbraRcpSinTheta;

            intersectsShadow = intersectsShadow || (distanceFromAxis < distanceAlongAxis*cullingData.penumbraTanTheta);
        }
    }
    return intersectsRange && intersectsShadow;
}

bool CapsuleShadowIntersectsTile(
    CapsuleShadowOccluder capsule,
    CapsuleShadowCaster caster,
    float3 tileCenterWS,
    float tileRadiusWS,
    float indirectRangeFactor)
{
    float3 tileToCapsuleVec = capsule.centerRWS - tileCenterWS;
    float capsuleSignedDistanceFromTile = CapsuleSignedDistance(tileToCapsuleVec, capsule.offset, capsule.axisDirWS, capsule.radius) - tileRadiusWS;

    bool intersectsTile = false;
    uint casterType = GetCasterType(caster);
	if (casterType == CAPSULESHADOWCASTERTYPE_INDIRECT)
	{
		intersectsTile = (capsuleSignedDistanceFromTile < capsule.radius*indirectRangeFactor);
	}
	else
	{
		CasterCullingData cullingData;
        if (CapsuleShadowCasterIntersectsTile(caster, tileCenterWS, tileRadiusWS, cullingData))
        {
            intersectsTile = CapsuleShadowVolumeIntersectsTile(
                tileToCapsuleVec,
                tileRadiusWS,
                capsuleSignedDistanceFromTile,
                capsule.axisDirWS,
                capsule.offset,
                capsule.radius,
                cullingData,
                caster.shadowRange);
		}
	}
    return intersectsTile;
}

#endif // ndef CAPSULE_SHADOWS_TILING_HLSL
