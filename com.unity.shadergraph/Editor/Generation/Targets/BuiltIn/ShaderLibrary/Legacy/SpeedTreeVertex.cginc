#ifndef SPEEDTREE_VERTEX_INCLUDED
#define SPEEDTREE_VERTEX_INCLUDED

///////////////////////////////////////////////////////////////////////
//  SpeedTree v6 Vertex Processing

///////////////////////////////////////////////////////////////////////
//  struct SpeedTreeVB

// texcoord setup
//
//      BRANCHES                        FRONDS                      LEAVES
// 0    diffuse uv, branch wind xy      "                           "
// 1    lod xyz, 0                      lod xyz, 0                  anchor xyz, lod scalar
// 2    detail/seam uv, seam amount, 0  frond wind xyz, 0           leaf wind xyz, leaf group

struct SpeedTreeVB
{
    float4 vertex       : POSITION;
    float4 tangent      : TANGENT;
    float3 normal       : NORMAL;
    float4 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float4 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    half4 color         : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


///////////////////////////////////////////////////////////////////////
//  SpeedTree winds

#ifdef ENABLE_WIND

#define WIND_QUALITY_NONE       0
#define WIND_QUALITY_FASTEST    1
#define WIND_QUALITY_FAST       2
#define WIND_QUALITY_BETTER     3
#define WIND_QUALITY_BEST       4
#define WIND_QUALITY_PALM       5

uniform half _WindQuality;
uniform half _WindEnabled;

#include "SpeedTreeWind.cginc"

#endif

///////////////////////////////////////////////////////////////////////
//  OffsetSpeedTreeVertex

void OffsetSpeedTreeVertex(inout SpeedTreeVB data, float lodValue)
{
    float3 finalPosition = data.vertex.xyz;

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
            rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)unity_ObjectToWorld));
            rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)unity_ObjectToWorld)) * _ST_WindBranchAnchor.w;
        }
    #endif

    #if defined(GEOM_TYPE_BRANCH) || defined(GEOM_TYPE_FROND)

        // smooth LOD
        #ifdef LOD_FADE_PERCENTAGE
            finalPosition = lerp(finalPosition, data.texcoord1.xyz, lodValue);
        #endif

        // frond wind, if needed
        #if defined(ENABLE_WIND) && defined(GEOM_TYPE_FROND)
            if (windQuality == WIND_QUALITY_PALM)
                finalPosition = RippleFrond(finalPosition, data.normal, data.texcoord.x, data.texcoord.y, data.texcoord2.x, data.texcoord2.y, data.texcoord2.z);
        #endif

    #elif defined(GEOM_TYPE_LEAF)

        // remove anchor position
        finalPosition -= data.texcoord1.xyz;

        bool isFacingLeaf = data.color.a == 0;
        if (isFacingLeaf)
        {
            #ifdef LOD_FADE_PERCENTAGE
                finalPosition *= lerp(1.0, data.texcoord1.w, lodValue);
            #endif
            // face camera-facing leaf to camera
            float offsetLen = length(finalPosition);
            finalPosition = mul(finalPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * finalPosition
            finalPosition = normalize(finalPosition) * offsetLen; // make sure the offset vector is still scaled
        }
        else
        {
            #ifdef LOD_FADE_PERCENTAGE
                float3 lodPosition = float3(data.texcoord1.w, data.texcoord3.x, data.texcoord3.y);
                finalPosition = lerp(finalPosition, lodPosition, lodValue);
            #endif
        }

        #ifdef ENABLE_WIND
            // leaf wind
            if (windQuality > WIND_QUALITY_FASTEST && windQuality < WIND_QUALITY_PALM)
            {
                float leafWindTrigOffset = data.texcoord1.x + data.texcoord1.y;
                finalPosition = LeafWind(windQuality == WIND_QUALITY_BEST, data.texcoord2.w > 0.0, finalPosition, data.normal, data.texcoord2.x, float3(0,0,0), data.texcoord2.y, data.texcoord2.z, leafWindTrigOffset, rotatedWindVector);
            }
        #endif

        // move back out to anchor
        finalPosition += data.texcoord1.xyz;

    #endif

    #ifdef ENABLE_WIND
        float3 treePos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

        #ifndef GEOM_TYPE_MESH
            if (windQuality >= WIND_QUALITY_BETTER)
            {
                // branch wind (applies to all 3D geometry)
                finalPosition = BranchWind(windQuality == WIND_QUALITY_PALM, finalPosition, treePos, float4(data.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
            }
        #endif

        if (windQuality > WIND_QUALITY_NONE)
        {
            // global wind
            finalPosition = GlobalWind(finalPosition, treePos, true, rotatedWindVector, _ST_WindGlobal.x);
        }
    #endif

    data.vertex.xyz = finalPosition;
}

#endif // SPEEDTREE_VERTEX_INCLUDED
