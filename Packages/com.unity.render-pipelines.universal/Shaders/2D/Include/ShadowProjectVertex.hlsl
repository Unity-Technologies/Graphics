#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

// ProjectShadow calls GetWorldToHClipMatrix(), so this header needs SpaceTransforms. It used to rely
// on the includer having pulled Core.hlsl in first, which is true of Shadow2DProjectedPass.hlsl but
// was not true of TestShadow2D-Complete.shader -- that fixture failed to compile with "undeclared
// identifier 'GetWorldToHClipMatrix'" and nothing noticed, because nothing compiled it. Core.hlsl is
// include-guarded, so declaring the dependency here is free where it is already satisfied.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define ToFloat(x) x
#define Deg2Rad(x) (x * 3.14159265359f / 180)

#define MIN_SHADOW_Y 0.000001f

struct Attributes
{
    float3 vertex       : POSITION;
    float4 packed0      : TANGENT;
    // Second outline neighbor of `vertex`. On a soft-fin vertex the pair
    // (packed0.zw, neighborNext) is (prevPt, v1 of the current edge); together
    // they describe the two edges bracketing this vertex along the silhouette
    // for the pole test in ProjectShadow. Zero on hard / interior vertices.
    float2 neighborNext : TEXCOORD0;
};

struct Varyings
{
    float4 vertex      : SV_POSITION;
    float2 shadow      : TEXCOORD0;

};

uniform float3 _LightPos;
uniform float4x4 _ShadowModelMatrix;    // This is a custom model matrix without scaling
uniform float4x4 _ShadowModelInvMatrix;
uniform float3 _ShadowModelScale;       // This is the scale
uniform float  _ShadowRadius;
uniform float  _ShadowContractionDistance;
uniform float  _SoftShadowAngle;

float AngleFromDir(float3 dir)
{
    // Assumes dir is normalized. Will return -180 to 180
    float angle = acos(dir.x);
    float gt180 = ceil(saturate(-dir.y));  // Greater than 180
    return gt180 * -angle + (1 - gt180) * angle;
}

float3 DirFromAngle(float angle)
{
    return float3(cos(angle), sin(angle), 0);
}

// Wedge-local parameterisation consumed by the fragment as
//   value = 1 - saturate(|shadow.x| / clamp(shadow.y, MIN_SHADOW_Y, 1))
// so .x is signed angular position across the fin wedge and .y is the wedge-width
// normaliser. v0 is (0,0); the outer corners are (-1,1) and (+1,1), giving full
// shadow along the wedge centre line and zero at both outer rays.
//
// shadowType 2 is the interior corner after the half-wedge collapse in
// ProjectShadow: it sits ON the centre line at full width, so it needs (0,1).
// Note (0,0) would NOT work - it makes .x/.y degenerate to 1 along the edge to
// the outer corner, zeroing the ramp across the whole triangle.
float2 CalculateShadowValue(float shadowType)
{
    float isLeft = ToFloat(shadowType == 1);
    float isRight = ToFloat(shadowType == 3);
    float isCenter = ToFloat(shadowType == 2);

    return float2(-isLeft + isRight, isLeft + isRight + isCenter);
}


float3 SoftShadowDir(float3 lightDir, float3 vertex0, float3 vertex1, float angleOp, float softShadowAngle)
{
    float lightAngle = AngleFromDir(lightDir);
    float edgeAngle = AngleFromDir(normalize(vertex1 - vertex0));
    float softAngle = lightAngle + angleOp * softShadowAngle;

    return DirFromAngle(softAngle);
}

float4 ProjectShadowVertexToWS(float2 vertex, float2 otherEndPt, float2 contractDir, float shadowType, float3 lightPos, float3 shadowModelScale, float4x4 shadowModelMatrix, float4x4 shadowModelInvMatrix, float shadowContractionDistance, float shadowRadius, float softShadowAngle)
{

    #if NOT_TRANSFORMABLE
        float3 vertexOS0 = float3(vertex.x, vertex.y, 0);
        float3 vertexOS1 = float3(otherEndPt.x, otherEndPt.y, 0);  // the tangent has the adjacent point stored in zw
        float3 lightPosOS = float3(lightPos.xy, 0);  // Transform the light into local space
    #else
        float3 vertexOS0 = float3(vertex.x * shadowModelScale.x, vertex.y * shadowModelScale.y, 0);
        float3 vertexOS1 = float3(otherEndPt.x * shadowModelScale.x, otherEndPt.y * shadowModelScale.y, 0);  // the tangent has the adjacent point stored in zw
        float3 lightPosOS = float3(mul(shadowModelInvMatrix, float4(lightPos.x, lightPos.y, lightPos.z, 1)).xy, 0);  // Transform the light into local space
    #endif

    float3 unnormalizedLightDir0 = vertexOS0 - lightPosOS;
    float3 unnormalizedLightDir1 = vertexOS1 - lightPosOS;

    float3 lightDir0 = normalize(unnormalizedLightDir0);
    float3 lightDir1 = normalize(unnormalizedLightDir1);
    float3 avgLightDir = normalize(lightDir0 + lightDir1);

    float isSoftShadow = ToFloat(shadowType >= 1);
    float isHardShadow = ToFloat(shadowType == 0);

    float isShadowVertex = saturate(isSoftShadow + isHardShadow);

    float3 softShadowDir = SoftShadowDir(lightDir0, vertexOS0, vertexOS1, shadowType - 2, softShadowAngle);
    float3 hardShadowDir = lightDir0;

    float3 shadowDir = isSoftShadow * softShadowDir + isHardShadow * hardShadowDir;

    float lightDistance = length(unnormalizedLightDir0);
    float hardShadowLength = max(shadowRadius / dot(lightDir0, avgLightDir), lightDistance);
    float softShadowLength = shadowRadius * (1 / cos(softShadowAngle));

    // Tests to make sure the light is between 0-90 degrees to the normal. Will be one if it is, zero if not.
    float3 shadowOffset =  (isSoftShadow * softShadowLength + isHardShadow * hardShadowLength) * shadowDir;
    float3 contractedVertexPos = vertexOS0 + float3(shadowContractionDistance * contractDir.xy, 0);

    // If we are suppose to extrude this point, then
    float3 finalVertexOS = isShadowVertex * (lightPosOS + shadowOffset) + (1 - isShadowVertex) * contractedVertexPos;

    #if NOT_TRANSFORMABLE
        return float4(finalVertexOS, 1);
    #else
        return mul(shadowModelMatrix, float4(finalVertexOS, 1));
    #endif
}


// Signed twice-area of triangle (A, B, L). Zero if collinear; sign flips
// depending on which side of A->B the point L lies. Winding-agnostic — the
// pole test only uses the SIGN AGREEMENT between two adjacent edges, so it
// works whether the outline is CCW or CW as long as consistent along the
// shape.
float ShadowEdgeFacingSign(float2 A, float2 B, float2 L)
{
    return (B.x - A.x) * (L.y - A.y) - (B.y - A.y) * (L.x - A.x);
}

Varyings ProjectShadow(Attributes v)
{
    Varyings o;

    float2 otherEndPt = v.packed0.zw;
    float  shadowType = v.packed0.x;
    float2 position = v.vertex.xy;
    float  softShadowAngle = _SoftShadowAngle;
    float2  contractDir = 0;

    // Soft-fin vertices only (shadowType 1 = SoftLeft, 3 = SoftRight). Two
    // separate decisions are made here, and they differ in scope:
    //
    //  1. Is this vertex a silhouette pole? Both corners of a fin triangle read
    //     the same (prevPt, v1) pair, so they always AGREE - essential, or a
    //     non-pole fin would only partly collapse. Non-poles collapse to a
    //     zero-area triangle the GPU discards before rasterization, skipping the
    //     relatively expensive soft extrusion + falloff sampling for ~N-2 of
    //     every N silhouette vertices on a convex caster.
    //  2. On a surviving pole fin, is this corner the interior one? Here the two
    //     corners deliberately DISAGREE - exactly one is interior - which is what
    //     turns the wedge into its exterior half.
    if (shadowType == 1 || shadowType == 3)
    {
        #if NOT_TRANSFORMABLE
            float2 lightXY = _LightPos.xy;
            float2 P       = position;
            float2 P_prev  = otherEndPt;
            float2 P_next  = v.neighborNext;
        #else
            float2 lightXY = mul(_ShadowModelInvMatrix, float4(_LightPos.xyz, 1)).xy;
            float2 P       = position   * _ShadowModelScale.xy;
            float2 P_prev  = otherEndPt * _ShadowModelScale.xy;
            float2 P_next  = v.neighborNext * _ShadowModelScale.xy;
        #endif

        float sgnPrev = ShadowEdgeFacingSign(P_prev, P,      lightXY);
        float sgnCurr = ShadowEdgeFacingSign(P,      P_next, lightXY);

        if (sgnPrev * sgnCurr >= 0)
        {
            // Not a silhouette pole for this light. Collapse to v0's outline
            // position (ProjectionNone path, contractDir==0). Matches where
            // the fin's corner vertex (shadowType==-1) rasterizes.
            #if NOT_TRANSFORMABLE
                o.vertex = mul(GetWorldToHClipMatrix(), float4(P, 0, 1));
            #else
                o.vertex = mul(GetWorldToHClipMatrix(), mul(_ShadowModelMatrix, float4(P, 0, 1)));
            #endif
            o.shadow = float2(0, 0);
            return o;
        }

        // Pole fin. The authored wedge spans +/- softShadowAngle about the hard
        // shadow direction, so half of it lies over the projected body. That
        // interior half is never wanted: at full shadow length it is hidden
        // beyond the light's range, but shortening the shadow brings it onto lit
        // pixels, and a stencil test against body coverage cannot remove it
        // because the interior corner extends radially PAST the body's far edge
        // (fins terminate at shadowRadius/cos(softShadowAngle) while each body
        // quad ends at max(shadowRadius/dot(lightDir0,avgLightDir), lightDistance)).
        //
        // Fix it geometrically: pull the interior corner onto the hard shadow
        // direction so the fin becomes exactly the exterior half-wedge, abutting
        // the body along the silhouette ray instead of straddling it.
        //
        // Which side is interior needs no winding knowledge: at a pole, BOTH
        // outline neighbours lie angularly on the same side of P as seen from the
        // light - that is what makes P a pole. Summing both cross products rather
        // than trusting one keeps the sign stable when a single neighbour happens
        // to lie nearly along the same ray from the light (thin sliver casters),
        // where that neighbour's cross alone would be near zero.
        float2 dirP    = P      - lightXY;
        float2 dirPrev = P_prev - lightXY;
        float2 dirNext = P_next - lightXY;
        float  crossPrev = dirP.x * dirPrev.y - dirP.y * dirPrev.x;
        float  crossNext = dirP.x * dirNext.y - dirP.y * dirNext.x;
        float  interiorSign = crossPrev + crossNext;

        // SoftRight (3) is extruded to lightAngle + softShadowAngle, so it sits on
        // the positive-cross side of dirP; SoftLeft (1) on the negative side.
        float mySide = (shadowType == 3) ? 1.0f : -1.0f;

        if (mySide * interiorSign > 0)
        {
            // shadowType 2 is the unused ProjectionType slot and the natural
            // centre of the angleOp = shadowType - 2 mapping: it extrudes along
            // the hard shadow direction and still counts as a soft vertex, so it
            // picks up softShadowLength. CalculateShadowValue maps it to (0,1) -
            // wedge centre at full width - so the surviving half ramps from full
            // shadow along the inner edge to zero at the outer corner.
            shadowType = 2;
        }
    }

    float4 positionWS = ProjectShadowVertexToWS(position, otherEndPt, contractDir, shadowType,  _LightPos, _ShadowModelScale, _ShadowModelMatrix, _ShadowModelInvMatrix, _ShadowContractionDistance, _ShadowRadius, softShadowAngle);
    o.vertex = mul(GetWorldToHClipMatrix(), positionWS);
    o.shadow = CalculateShadowValue(shadowType);
    return o;
}

#endif
