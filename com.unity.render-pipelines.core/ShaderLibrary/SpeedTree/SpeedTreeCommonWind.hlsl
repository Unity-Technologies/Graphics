#ifndef HDRP_SPEEDTREE_COMMON_WIND_INCLUDED
#define HDRP_SPEEDTREE_COMMON_WIND_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpeedTree/SpeedTreeWind.hlsl"

#if defined(SPEEDTREE_V7) && (EFFECT_BILLBOARD)

CBUFFER_START(UnityBillboardPerCamera)
float3 unity_BillboardNormal;
float3 unity_BillboardTangent;
float4 unity_BillboardCameraParams;
#define unity_BillboardCameraPosition (unity_BillboardCameraParams.xyz)
#define unity_BillboardCameraXZAngle (unity_BillboardCameraParams.w)
CBUFFER_END

CBUFFER_START(UnityBillboardPerBatch)
float4 unity_BillboardInfo; // x: num of billboard slices; y: 1.0f / (delta angle between slices)
float4 unity_BillboardSize; // x: width; y: height; z: bottom
float4 unity_BillboardImageTexCoords[16];
CBUFFER_END

#endif

#ifdef SPEEDTREE_V7

void Speedtree7Wind(float4 color, float4 texcoord0, float4 texcoord1, float4 texcoord2, float4 texcoord3, inout float3 finalPosition, inout float3 finalNormal)
{
#ifdef ENABLE_WIND
    half windQuality = _WindQuality * _WindEnabled;

    float3 rotatedWindVector, rotatedBranchAnchor;
    if (windQuality <= WIND_QUALITY_NONE)
    {
        rotatedWindVector = float3(0.0f, 0.0f, 0.0f);
        rotatedBranchAnchor = float3(0.0f, 0.0f, 0.0f);
    }
    else
    {
        // compute rotated wind parameters
        rotatedWindVector = SafeNormalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
        rotatedBranchAnchor = SafeNormalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)UNITY_MATRIX_M)) * _ST_WindBranchAnchor.w;
    }
#endif

#if defined(GEOM_TYPE_BRANCH) || defined (GEOM_TYPE_BRANCH_DETAIL) || defined(GEOM_TYPE_FROND)

    // smooth LOD
#ifdef LOD_FADE_PERCENTAGE
    finalPosition = lerp(finalPosition, texcoord1.xyz, unity_LODFade.x);
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
        finalPosition *= lerp(1.0, texcoord1.w, unity_LODFade.x);
#endif
        // face camera-facing leaf to camera
        float offsetLen = length(finalPosition);
        float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
        //finalPosition = mul(finalPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * finalPosition
        finalPosition = mul(mtx_ITMV, float4(finalPosition.xyz, 0)).xyz;
        finalPosition = SafeNormalize(finalPosition) * offsetLen; // make sure the offset vector is still scaled
    }
    else
    {
#ifdef LOD_FADE_PERCENTAGE
        float3 lodPosition = float3(texcoord1.w, texcoord3.x, texcoord3.y);
        finalPosition = lerp(finalPosition, lodPosition, unity_LODFade.x);
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

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    treePos += _WorldSpaceCameraPos;
#endif

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
}

#endif // SPEEDTREE_V7

#if defined(SPEEDTREE_V7) && defined(EFFECT_BILLBOARD)
void Speedtree7BBWind(float4 texcoord0, float4 texcoord1, float4 texcoord2, float4 texcoord3, inout float3 finalPosition, inout float3 finalNormal, inout float3 finalTangent, inout float2 finalUV)
{
    // This is handling Speedtree v7 billboards
    float4x4 objToWorld = UNITY_MATRIX_M;
    // assume no scaling & rotation
    finalPosition.xyz += float3(objToWorld[0].w, objToWorld[1].w, objToWorld[2].w);
    float3 worldPos = finalPosition.xyz;

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    worldPos += _WorldSpaceCameraPos;
#endif

#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 eyeVec = normalize(unity_BillboardCameraPosition - worldPos);
    float3 billboardTangent = normalize(float3(-eyeVec.z, 0, eyeVec.x));            // cross(eyeVec, {0,1,0})
    float3 billboardNormal = float3(billboardTangent.z, 0, -billboardTangent.x);    // cross({0,1,0},billboardTangent)
    float3 angle = atan2(billboardNormal.z, billboardNormal.x);                     // signed angle between billboardNormal to {0,0,1}
    angle += angle < 0 ? 2 * SPEEDTREE_PI : 0;
#else
    float3 billboardTangent = unity_BillboardTangent;
    float3 billboardNormal = unity_BillboardNormal;
    float angle = unity_BillboardCameraXZAngle;
#endif

    float widthScale = texcoord1.x;
    float heightScale = texcoord1.y;
    float rotation = texcoord1.z;

    float2 percent = texcoord0.xy;
    float3 billboardPos = (percent.x - 0.5f) * unity_BillboardSize.x * widthScale * billboardTangent;
    billboardPos.y += (percent.y * unity_BillboardSize.y + unity_BillboardSize.z) * heightScale;

    half windQuality = _WindQuality * _WindEnabled;

#ifdef ENABLE_WIND
    if (windQuality > 0)
        billboardPos = GlobalWind(billboardPos, worldPos, true, _ST_WindVector.xyz, texcoord1.w);
#endif

    finalPosition.xyz = billboardPos + worldPos;
    finalNormal = billboardNormal.xyz;
    finalTangent = billboardTangent.xyz;

    float slices = unity_BillboardInfo.x;
    float invDelta = unity_BillboardInfo.y;
    angle += rotation;

    float imageIndex = fmod(floor(angle * invDelta + 0.5f), slices);
    float4 imageTexCoords = unity_BillboardImageTexCoords[imageIndex];
    if (imageTexCoords.w < 0)
    {
        finalUV = imageTexCoords.xy - imageTexCoords.zw * percent.yx;
    }
    else
    {
        finalUV = imageTexCoords.xy + imageTexCoords.zw * percent;
    }
}
#endif // SPEEDTREE_V7 && EFFECT_BILLBOARD

#ifdef SPEEDTREE_V8
void Speedtree8Wind(float4 texcoord0, float4 texcoord1, float4 texcoord2, float4 texcoord3, inout float3 finalPosition, inout float3 finalNormal, inout float3 finalTangent, inout float2 finalUV, inout float finalAlpha)
{
    // smooth LOD
#if defined(LOD_FADE_PERCENTAGE) && !defined(EFFECT_BILLBOARD)
    finalPosition.xyz = lerp(finalPosition.xyz, texcoord2.xyz, unity_LODFade.x);
#endif

    // wind
#if defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
    if (_WindEnabled > 0)
    {
        float3 rotatedWindVector = SafeNormalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
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
        float geometryType = (int)(texcoord3.w + 0.25);
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

#endif // !defined(EFFECT_BILLBOARD)

        // global wind
        float globalWindTime = _ST_WindGlobal.x;
#if defined(EFFECT_BILLBOARD) && defined(UNITY_INSTANCING_ENABLED)
        globalWindTime += UNITY_ACCESS_INSTANCED_PROP(STWind, _GlobalWindTime);
#endif
        windyPosition = GlobalWind(windyPosition, treePos, true, rotatedWindVector, globalWindTime);
        finalPosition.xyz = windyPosition;
    }
#endif	// defined(ENABLE_WIND) && !defined(_WINDQUALITY_NONE)
}

void Speedtree8BBFade(float4 texcoord0, inout float3 finalPosition, inout float3 finalNormal, inout float3 finalTangent, inout float2 finalUV, inout float finalAlpha)
{
    float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    treePos += _WorldSpaceCameraPos;
#endif
    // crossfade faces
    bool topDown = (texcoord0.z > 0.5);
    float4x4 mtx_ITMV = transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V));
    float3 viewDir = mtx_ITMV[2].xyz;
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    float3 cameraDir = normalize(_WorldSpaceCameraPos - treePos);
#else
    float3 cameraDir = normalize(mul((float3x3)UNITY_MATRIX_M, _WorldSpaceCameraPos - treePos));
#endif
    float viewDot = max(dot(viewDir, finalNormal), dot(cameraDir, finalNormal));
    viewDot *= viewDot;
    viewDot *= viewDot;
    viewDot += topDown ? 0.38 : 0.18; // different scales for horz and vert billboards to fix transition zone

                                      // if invisible, avoid overdraw
    if (viewDot < 0.3333)
    {
        finalPosition.xyz = float3(0, 0, 0);
    }

    // TODO -- add output alpha
    finalAlpha = clamp(viewDot, 0, 1);

    // adjust lighting on billboards to prevent seams between the different faces
    if (topDown)
    {
        finalNormal += cameraDir;
        float3 binormal = -cross(finalNormal, finalTangent.xyz);
        finalTangent = normalize(cross(finalNormal, binormal));
    }
    else
    {
        // We do normally have the ability to use the w component in the tangent to denote flip/no-flip, but
        // within shadergraph, we apparently cannot access that.
        //float3 binormal = cross(finalNormal, tangent.xyz) * tangent.w;
        float3 binormal = -cross(finalNormal, finalTangent.xyz);        // Assuming that billboards always have a -1 in the w
        float3 right = cross(cameraDir, binormal);
        finalNormal = cross(binormal, right);
    }

    finalNormal = normalize(finalNormal);
}
#endif

void ApplyWindTransformation(float3 vertex, float3 normal, float3 tangent, float4 color, float4 texcoord0, float4 texcoord1, float4 texcoord2, float4 texcoord3, out float3 finalPosition, out float3 finalNormal, out float3 finalTangent, out float2 finalUV, out float finalAlpha)
{
    finalPosition = vertex.xyz;
    finalNormal = normal.xyz;
    finalTangent = tangent.xyz;
    finalUV = texcoord0.xy;     // Billboard UVs (Speedtree7) might overwrite this.
    finalAlpha = 1.0;           // Billboards in Speedtree8 will change this.

#if defined(SPEEDTREE_V7) && !defined(EFFECT_BILLBOARD)

    Speedtree7Wind(color, texcoord0, texcoord1, texcoord2, texcoord3, finalPosition, finalNormal);

#elif defined(SPEEDTREE_V7)

    Speedtree7BBWind(texcoord0, texcoord1, texcoord2, texcoord3, finalPosition, finalNormal, finalTangent, finalUV);

#elif defined(SPEEDTREE_V8)

    Speedtree8Wind(texcoord0, texcoord1, texcoord2, texcoord3, finalPosition, finalNormal, finalTangent, finalUV, finalAlpha);

#if defined(EFFECT_BILLBOARD)
    Speedtree8BBFade(texcoord0, finalPosition, finalNormal, finalTangent, finalUV, finalAlpha);
#endif // defined(EFFECT_BILLBOARD)

#endif 
}

#endif
