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
                        if (param.name == "cone_" + nameof(TCone.radius0))
                            baseRadius = param.exp;
                        if (param.name == "cone_" + nameof(TCone.radius1))
                            topRadius = param.exp;

                        continue; //exclude all cone inputs
                    }
                    yield return param;
                }

                VFXExpression tanSlope = (baseRadius - topRadius) / height; //N.B: Not the same direction than PositionCone
                VFXExpression slope = new VFXExpressionATan(tanSlope);


                var finalTransform = transform; //Can't really integrate radius0/1 the value could be 0
                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                VFXExpression scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
                scale = scale * VFXOperatorUtility.TwoExpression[UnityEngine.VFX.VFXValueType.Float3];
                yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invFieldScale");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");

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
float3 tPos = mul(invFieldTransform, float4(nextPos, 1.0f)).xyz;
float cone_radius = lerp(cone_baseRadius, cone_topRadius, saturate(tPos.y/cone_height));
float cone_halfHeight = cone_height * 0.5f;
float sqrLength = dot(tPos.xz, tPos.xz);
";

                if (mode == Mode.Solid)
                    Source += @"
bool collision = abs(tPos.y - cone_halfHeight) < cone_halfHeight && sqrLength < cone_radius * cone_radius;";
                else
                    Source += @"
bool collision = abs(tPos.y - cone_halfHeight) > cone_halfHeight || sqrLength > cone_radius * cone_radius;";

                Source += @"
if (collision)
{
    float dist = sqrt(sqrLength);
    float distToCap = colliderSign * (cone_halfHeight - abs(tPos.y - cone_halfHeight));
    float distToSide = colliderSign * (cone_radius - dist);";

                //Position/Normal correction
                if (mode == Mode.Solid)
                    Source += @"
    float3 n =  distToSide < distToCap
                ? normalize(float3(tPos.x * sincosSlope.y, sincosSlope.x, tPos.z * sincosSlope.y))
                : float3(0, tPos.y < cone_halfHeight ? -1.0f : 1.0f, 0);
    tPos += n * min(distToSide, distToCap);";
                else
                    Source += @"
    float3 n = distToSide > distToCap
        ? -normalize(float3(tPos.x * sincosSlope.y, sincosSlope.x, tPos.z * sincosSlope.y))
        : -float3(0, tPos.y < cone_halfHeight ? -1.0f : 1.0f, 0);
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
