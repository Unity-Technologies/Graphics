using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Collision")]
    class CollisionSphere : CollisionBase
    {
        public override string name { get { return "Collide with Sphere"; } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere with which particles can collide.")]
            public TSphere sphere = TSphere.defaultValue;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression transform = null;
                VFXExpression radius = null;

                foreach (var param in base.parameters)
                {
                    if (param.name.StartsWith("sphere"))
                    {
                        if (param.name == "sphere_transform")
                            transform = param.exp;
                        if (param.name == "sphere_radius")
                            radius = param.exp;

                        continue; //exclude all sphere inputs
                    }
                    yield return param;
                }

                var zero = VFXOperatorUtility.ZeroExpression[UnityEngine.VFX.VFXValueType.Float3];
                var radiusScale = new VFXExpressionTRSToMatrix(zero, zero, new VFXExpressionCombine(radius, radius, radius));

                var finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);
                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");

                //TRS + Scale is it correct ? //TODOPAUL
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 tPos = mul(invFieldTransform, float4(nextPos, 1.0f)).xyz;
float sqrLength = dot(tPos, tPos);
if (colliderSign * sqrLength <= colliderSign)
{
    float dist = sqrt(sqrLength);
    float3 n = colliderSign * tPos / dist;
    tPos -= n * (dist - 1.0f) * colliderSign;

    position = mul(fieldTransform, float4(tPos.xyz, 1.0f));
    n = VFXSafeNormalize(mul(float4(n, 0.0f), invFieldTransform));
";

                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
