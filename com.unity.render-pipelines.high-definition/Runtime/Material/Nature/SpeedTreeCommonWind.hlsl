#ifndef HDRP_SPEEDTREE_COMMON_WIND_INCLUDED
#define HDRP_SPEEDTREE_COMMON_WIND_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTreeWind.hlsl"

void ApplyWindTransformation(float3 vertex, float3 normal, float4 color, float4 texcoord0, float4 texcoord1, float4 texcoord2, float4 texcoord3, float lodValue, float geometryType, out float3 finalPosition, out float3 finalNormal, out float2 finalUV)
{
    finalPosition = vertex.xyz;
    finalNormal = normal.xyz;
    finalUV = texcoord0.xy;     // Billboard UVs might overwrite this.
#ifdef SPEEDTREE_V7
#ifdef ENABLE_WIND
    //half windQuality = _WindQuality * _WindEnabled;
    half windQuality = _WindQuality;

    float3 rotatedWindVector, rotatedBranchAnchor;
    if (windQuality <= WIND_QUALITY_NONE)
    {
        rotatedWindVector = float3(0.0f, 0.0f, 0.0f);
        rotatedBranchAnchor = float3(0.0f, 0.0f, 0.0f);
    }
    else
    {
        // compute rotated wind parameters
        rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
        rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)UNITY_MATRIX_M)) * _ST_WindBranchAnchor.w;
    }
#endif

#if defined(GEOM_TYPE_BRANCH) || defined(GEOM_TYPE_FROND)

    // smooth LOD
#ifdef LOD_FADE_PERCENTAGE
    finalPosition = lerp(finalPosition, texcoord1.xyz, lodValue);
#endif

    // frond wind, if needed
#if defined(ENABLE_WIND) && defined(GEOM_TYPE_FROND)
    if (windQuality == WIND_QUALITY_PALM)
        finalPosition = RippleFrond(finalPosition, finalNormal, texcoord0.x, texcoord0.y, texcoord2.x, texcoord2.y, texcoord2.z);
#endif

#elif defined(GEOM_TYPE_LEAF)

    // remove anchor position
    finalPosition -= texcoord1.xyz;

    bool isFacingLeaf = color.a == 0;
    if (isFacingLeaf)
    {
#ifdef LOD_FADE_PERCENTAGE
        finalPosition *= lerp(1.0, texcoord1.w, lodValue);
#endif
        // face camera-facing leaf to camera
        float offsetLen = length(finalPosition);
        float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
        //finalPosition = mul(finalPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * finalPosition
        finalPosition = mul(mtx_ITMV, float4(finalPosition.xyz, 0)).xyz;
        finalPosition = normalize(finalPosition) * offsetLen; // make sure the offset vector is still scaled
    }
    else
    {
#ifdef LOD_FADE_PERCENTAGE
        float3 lodPosition = float3(texcoord1.w, texcoord3.x, texcoord3.y);
        finalPosition = lerp(finalPosition, lodPosition, lodValue);
#endif
    }

#ifdef ENABLE_WIND
    // leaf wind
    if (windQuality > WIND_QUALITY_FASTEST && windQuality < WIND_QUALITY_PALM)
    {
        float leafWindTrigOffset = texcoord1.x + texcoord1.y;
        finalPosition = LeafWind(windQuality == WIND_QUALITY_BEST, texcoord2.w > 0.0, finalPosition, finalNormal, texcoord2.x, float3(0, 0, 0), texcoord2.y, texcoord2.z, leafWindTrigOffset, rotatedWindVector);
    }
#endif

    // move back out to anchor
    finalPosition += texcoord1.xyz;

#endif

#ifdef ENABLE_WIND
    float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);

#ifndef GEOM_TYPE_MESH
    if (windQuality >= WIND_QUALITY_BETTER)
    {
        // branch wind (applies to all 3D geometry)
        finalPosition = BranchWind(windQuality == WIND_QUALITY_PALM, finalPosition, treePos, float4(texcoord0.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
    }
#endif

    if (windQuality > WIND_QUALITY_NONE)
    {
        // global wind
        finalPosition = GlobalWind(finalPosition, treePos, true, rotatedWindVector, _ST_WindGlobal.x);
    }
#endif

    //vertex.xyz = finalPosition;
#else // if it's SPEEDTREE_V8

    // smooth LOD
#if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
    finalPosition.xyz = lerp(finalPosition.xyz, texcoord2.xyz, lodValue);
#endif

    // wind
#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    if (_WindEnabled > 0)
    {
        float3 rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
        float windLength = length(rotatedWindVector);
        if (windLength < 1e-5)
        {
            // sanity check that wind data is available
            return;
        }
        rotatedWindVector /= windLength;

        float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
        float3 windyPosition = finalPosition.xyz;

#ifndef EFFECT_BILLBOARD
        // geometry type
        geometryType = (int)(texcoord3.w + 0.25);
        bool leafTwo = false;
        if (geometryType > GEOM_TYPE_FACINGLEAF)
        {
            geometryType -= 2;
            leafTwo = true;
        }

        // leaves
        if (geometryType > GEOM_TYPE_FROND)
        {
            // remove anchor position
            float3 anchor = float3(texcoord1.zw, texcoord2.w);
            windyPosition -= anchor;

            if (geometryType == GEOM_TYPE_FACINGLEAF)
            {
                // face camera-facing leaf to camera
                float offsetLen = length(windyPosition);
                float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
                //windyPosition = mul(windyPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * windyPosition
                windyPosition = mul(mtx_ITMV, float4(windyPosition.xyz, 0)).xyz;
                windyPosition = normalize(windyPosition) * offsetLen; // make sure the offset vector is still scaled
            }

            // leaf wind
#if defined(_WINDQUALITY_FAST) || defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST)
#ifdef _WINDQUALITY_BEST
            bool bBestWind = true;
#else
            bool bBestWind = false;
#endif
            float leafWindTrigOffset = anchor.x + anchor.y;
            windyPosition = LeafWind(bBestWind, leafTwo, windyPosition, finalNormal, texcoord3.x, float3(0, 0, 0), texcoord3.y, texcoord3.z, leafWindTrigOffset, rotatedWindVector);
#endif

            // move back out to anchor
            windyPosition += anchor;
        }


        // frond wind
        bool bPalmWind = false;
#ifdef _WINDQUALITY_PALM
        bPalmWind = true;
        if (geometryType == GEOM_TYPE_FROND)
        {
            windyPosition = RippleFrond(windyPosition, finalNormal, finalUV.x, finalUV.y, texcoord3.x, texcoord3.y, texcoord3.z);
        }
#endif

        // branch wind (applies to all 3D geometry)
#if defined(_WINDQUALITY_BETTER) || defined(_WINDQUALITY_BEST) || defined(_WINDQUALITY_PALM)
        float3 rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)UNITY_MATRIX_M)) * _ST_WindBranchAnchor.w;
        windyPosition = BranchWind(bPalmWind, windyPosition, treePos, float4(texcoord0.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
#endif

#endif // !EFFECT_BILLBOARD

        // global wind
        float globalWindTime = _ST_WindGlobal.x;
#if defined(EFFECT_BILLBOARD) && defined(UNITY_INSTANCING_ENABLED)
        globalWindTime += UNITY_ACCESS_INSTANCED_PROP(STWind, _GlobalWindTime);
#endif
        windyPosition = GlobalWind(windyPosition, treePos, true, rotatedWindVector, globalWindTime);
        finalPosition.xyz = windyPosition;
    }
#endif	// defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)

#if defined(EFFECT_BILLBOARD)
    float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
    // crossfade faces
    bool topDown = (texcoord0.z > 0.5);
    float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
    float3 viewDir = mtx_ITMV[2].xyz;
    float3 cameraDir = normalize(mul((float3x3)UNITY_MATRIX_M, _WorldSpaceCameraPos - treePos));
    float viewDot = max(dot(viewDir, finalNormal), dot(cameraDir, finalNormal));
    viewDot *= viewDot;
    viewDot *= viewDot;
    viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone

                                      // if invisible, avoid overdraw
    if (viewDot < 0.3333)
    {
        finalPosition.xyz = float3(0, 0, 0);
    }

    //input.color = float4(1, 1, 1, clamp(viewDot, 0, 1));

    // adjust lighting on billboards to prevent seams between the different faces
//    if (topDown)
//    {
        finalNormal += cameraDir;
//    }
//    else
//    {
//        half3 binormal = cross(finalNormal, input.tangent.xyz) * input.tangent.w;
//        float3 right = cross(cameraDir, binormal);
//        finalNormal = cross(binormal, right);
//    }

    finalNormal = normalize(finalNormal);
#endif

#endif
}

#endif
