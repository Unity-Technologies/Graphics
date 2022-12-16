#if !defined(SHADOW_PROJECT_VERTEX)
#define SHADOW_PROJECT_VERTEX

#define ToFloat(x) x
#define Deg2Rad(x) (x * 3.14159265359f / 180)

struct Attributes
{
    float3 vertex  : POSITION;
    float4 packed0 : TANGENT;
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

float2 CalculateShadowValue(float shadowType)
{
    float isLeft = ToFloat(shadowType == 1);
    float isRight = ToFloat(shadowType == 3);

    return float2(-isLeft + isRight, isLeft + isRight);
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
    float3 vertexOS0 = float3(vertex.x * shadowModelScale.x, vertex.y * shadowModelScale.y, 0);
    float3 vertexOS1 = float3(otherEndPt.x * shadowModelScale.x, otherEndPt.y * shadowModelScale.y, 0);  // the tangent has the adjacent point stored in zw
    float3 lightPosOS = float3(mul(shadowModelInvMatrix, float4(lightPos.x, lightPos.y, lightPos.z, 1)).xy, 0);  // Transform the light into local space

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

    return mul(shadowModelMatrix, float4(finalVertexOS, 1));
}


Varyings ProjectShadow(Attributes v)
{
    Varyings o;

    float2 contractDir = v.packed0.xy;
    float2 otherEndPt = v.packed0.zw;
    float  shadowType = v.vertex.z;
    float2 position = v.vertex.xy;
    float  softShadowAngle = _SoftShadowAngle;

    float4 positionWS = ProjectShadowVertexToWS(position, otherEndPt, contractDir, shadowType,  _LightPos, _ShadowModelScale, _ShadowModelMatrix, _ShadowModelInvMatrix, _ShadowContractionDistance, _ShadowRadius, softShadowAngle);
    o.vertex = mul(GetWorldToHClipMatrix(), positionWS);
    o.shadow = CalculateShadowValue(shadowType);
    return o;
}

#endif
