using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionCone : CollisionBase
    {
        public override string name { get { return "Collide with Cone"; } }

        public class InputProperties
        {
            [Tooltip("Sets the cone with which particles can collide.")]
            public TCone cone = TCone.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression transform = null;
                VFXExpression height = null;
                VFXExpression baseRadius = null;
                VFXExpression topRadius = null;

                foreach (var param in base.parameters)
                {
                    if (param.name.StartsWith("cone"))
                    {
                        if (param.name == "cone_" + nameof(TCone.transform))
                            transform = param.exp;
                        if (param.name == "cone_" + nameof(TCone.height))
                            height = param.exp;
                        if (param.name == "cone_" + nameof(TCone.baseRadius))
                            baseRadius = param.exp;
                        if (param.name == "cone_" + nameof(TCone.topRadius))
                            topRadius = param.exp;

                        continue; //exclude all automatic cone inputs
                    }
                    yield return param;
                }

                var finalTransform = transform;

                var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
                yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");
                if (radiusMode != RadiusMode.None)
                {
                    var scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invFieldScale");
                }

                yield return new VFXNamedExpression(baseRadius, "cone_baseRadius");
                yield return new VFXNamedExpression(topRadius, "cone_topRadius");
                yield return new VFXNamedExpression(height, "cone_height");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
if (isZeroScaled)
    return;

float3 nextPos = position + velocity * deltaTime;
float3 tNextPos = mul(invFieldTransform, float4(nextPos, 1.0f)).xyz;
float cone_radius = lerp(cone_baseRadius, cone_topRadius, saturate(tNextPos.y/cone_height));
float cone_halfHeight = cone_height * 0.5f;
float relativePosY = abs(tNextPos.y - cone_halfHeight);
";

                if (radiusMode == RadiusMode.None)
                {
                    Source += @"
float sqrLength = dot(tNextPos.xz, tNextPos.xz);";
                    if (mode == Mode.Solid)
                        Source += @"
bool collision = relativePosY < cone_halfHeight && sqrLength < cone_radius * cone_radius;";
                    else
                        Source += @"
bool collision = relativePosY > cone_halfHeight || sqrLength > cone_radius * cone_radius;";

                    Source += @"
if (collision)
{
    float dist = sqrt(sqrLength);
    float radiusCorrectionXZ = 0.0f;
    float radiusCorrectionY = 0.0f;";
                }
                else
                {
                    Source += @"
float dist = max(length(tNextPos.xz), VFX_EPSILON);

float2 relativeScaleXZ = (tNextPos.xz/dist) * invFieldScale.xz;
float radiusCorrectionXZ = radius * length(relativeScaleXZ);
dist -= radiusCorrectionXZ * colliderSign;

float radiusCorrectionY = radius * invFieldScale.y;
relativePosY -= radiusCorrectionY * colliderSign;";
                    if (mode == Mode.Solid)
                        Source += @"
bool collision = relativePosY < cone_halfHeight && dist < cone_radius;";
                    else
                        Source += @"
bool collision = relativePosY > cone_halfHeight || dist > cone_radius;";

                    Source += @"
if (collision)
{";
                }

                Source += @"
    float distToCap = colliderSign * (cone_halfHeight - relativePosY);
    float distToSide = colliderSign * (cone_radius - dist);
    float3 tPos = mul(invFieldTransform, float4(position, 1.0f)).xyz;

    tNextPos.xz /= dist;
    float3 sideNormal = normalize(float3(tNextPos.x * cone_height, cone_baseRadius - cone_topRadius, tNextPos.z * cone_height));
    float3 capNormal = float3(0, tNextPos.y < cone_halfHeight ? -1.0f : 1.0f, 0);
    float3 n = (float3)0;";

                //Position/Normal correction, the reason behind float3(1,0,1) is the distToSide which is actually in 2D Plane (XZ)
                Source += @"
    if (colliderSign * distToSide < colliderSign * distToCap)
    {
        n = colliderSign * sideNormal;
        tPos += n * float3(1,0,1) * distToSide;
    }
    else
    {
        n = colliderSign * capNormal;
        tPos += n * distToCap;
    }";

                //Clamp outside/inside cone afterwards (could optional, only relevant with teleport cases)
                //Alternatively, we can apply several time Position & Normal correction
                bool applyClamp = true;
                if (applyClamp)
                {
                    Source += @"
    dist = max(length(tPos.xz), VFX_EPSILON);
    cone_radius = lerp(cone_baseRadius, cone_topRadius, saturate(tPos.y/cone_height));";
                    if (mode == Mode.Solid)
                    {
                        Source += @"
    if (    tPos.y > -radiusCorrectionY
        &&  tPos.y < cone_height + radiusCorrectionY
        &&  dist < cone_radius + radiusCorrectionXZ)
    {
        float3 candidateA = tPos;
        float3 candidateB = tPos;
        candidateA.y = tPos.y > 0.5f ? -radiusCorrectionY : cone_height + radiusCorrectionY;
        candidateB.xz = tPos.xz / dist * (cone_radius + radiusCorrectionXZ);
        if (Length2(candidateA - tPos.xyz) < Length2(candidateB - tPos.xyz))
            tPos = candidateA;
        else
            tPos = candidateB;
    }";
                    }
                    else
                    {
                        //Cone Radius is recomputed after y correction in case of high slope of the cone (no-op with cylinder)
                        Source += @"
    tPos.y = clamp(tPos.y, radiusCorrectionY, cone_height - radiusCorrectionY);
    cone_radius = lerp(cone_baseRadius, cone_topRadius, tPos.y/cone_height);
    tPos.xz = tPos.xz/dist * min(dist, cone_radius - radiusCorrectionXZ);";
                    }
                }

                //Back to the initial space
                Source += @"
    position = mul(fieldTransform, float4(tPos.xyz, 1.0f)).xyz;
    n = VFXSafeNormalize(mul(float4(n, 0.0f), invFieldTransform).xyz);";
                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
