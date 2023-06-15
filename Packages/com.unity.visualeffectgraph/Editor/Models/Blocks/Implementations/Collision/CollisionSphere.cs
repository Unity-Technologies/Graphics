using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-CollideWithSphere")]
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

                        continue; //exclude all automatic sphere inputs
                    }
                    yield return param;
                }

                VFXExpression finalTransform;

                //Integrate directly the radius into the common transform matrix
                var radiusScale = VFXOperatorUtility.UniformScaleMatrix(radius);
                finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);

                var isZeroScaled = VFXOperatorUtility.IsTRSMatrixZeroScaled(finalTransform);
                yield return new VFXNamedExpression(isZeroScaled, "isZeroScaled");

                yield return new VFXNamedExpression(finalTransform, "fieldTransform");
                yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(finalTransform), "invFieldTransform");
                if (radiusMode != RadiusMode.None)
                {
                    var scale = new VFXExpressionExtractScaleFromMatrix(finalTransform);
                    yield return new VFXNamedExpression(VFXOperatorUtility.Reciprocal(scale), "invFieldScale");
                }
            }
        }

        public override string source
        {
            get
            {
                var Source = @"
if (isZeroScaled)
    return;

float3 nextPos = position + velocity * deltaTime;
float3 tNextPos = mul(invFieldTransform, float4(nextPos, 1.0f)).xyz;
";
                if (radiusMode == RadiusMode.None)
                {
                    //radius == 0.0f, we could avoid a sqrt before the branch
                    Source += @"
float sqrLength  = dot(tNextPos, tNextPos);
if (colliderSign * sqrLength <= colliderSign)
{
    float dist = sqrt(sqrLength);";
                }
                else
                {
                    //The computation of relativeScale is pretty naive See : https://github.cds.internal.unity3d.com/unity/vfx-graphics/pull/258/files#r162621
                    //We can potentially take inspirations from https://iquilezles.org/www/articles/ellipsoids/ellipsoids.htm to improve these collisions
                    Source += @"
float dist = max(length(tNextPos), VFX_EPSILON);
float3 relativeScale = (tNextPos/dist) * invFieldScale;
float radiusCorrection = radius * length(relativeScale);
dist -= radiusCorrection * colliderSign;
if (colliderSign * dist <= colliderSign)
{";
                }
                Source += @"
    float3 n = colliderSign * (tNextPos/dist);
    float3 tPos = mul(invFieldTransform, float4(position, 1.0f)).xyz;
    tPos -= n * (dist - 1.0f) * colliderSign;
    position = mul(fieldTransform, float4(tPos.xyz, 1.0f)).xyz;
    n = VFXSafeNormalize(mul(float4(n, 0.0f), invFieldTransform).xyz);
";

                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
