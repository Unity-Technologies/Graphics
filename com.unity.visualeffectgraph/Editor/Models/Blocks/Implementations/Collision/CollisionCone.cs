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

                //Not the same direction than PositionCone, it's a real normal here.
                VFXExpression tanSlope = (baseRadius - topRadius) / height;
                VFXExpression slope = new VFXExpressionATan(tanSlope);

                var finalTransform = transform;
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

                yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
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
    float dist = sqrt(sqrLength);";
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
    float3 tPos = mul(invFieldTransform, float4(position, 1.0f)).xyz;";

                //Position/Normal correction
                if (mode == Mode.Solid)
                    Source += @"
    float3 n = distToSide < distToCap
                ? normalize(float3(tNextPos.x * sincosSlope.y, sincosSlope.x, tNextPos.z * sincosSlope.y))
                : float3(0, tNextPos.y < cone_halfHeight ? -1.0f : 1.0f, 0);
    tPos += n * min(distToSide, distToCap);";
                else
                    Source += @"
    float3 n = distToSide > distToCap
        ? -normalize(float3(tNextPos.x * sincosSlope.y, sincosSlope.x, tNextPos.z * sincosSlope.y))
        : -float3(0, tNextPos.y < cone_halfHeight ? -1.0f : 1.0f, 0);
    tPos += n * max(distToSide, distToCap);";

                //Back to initial space
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
