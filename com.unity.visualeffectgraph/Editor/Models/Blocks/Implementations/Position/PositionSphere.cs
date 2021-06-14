using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionSphere : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Arc Sphere"); } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere used for positioning the particles.")]
            public TArcSphere arcSphere = TArcSphere.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlots = GetExpressionsFromSlots(this);
                foreach (var p in allSlots.Where(e => e.name == "arcSphere_arc"
                    || e.name == "arcSequencer"))
                    yield return p;

                var transform = allSlots.FirstOrDefault(o => o.name == "arcSphere_sphere_transform").exp;
                var thickness = allSlots.FirstOrDefault(o => o.name == "Thickness").exp;
                var radius = allSlots.FirstOrDefault(o => o.name == "arcSphere_sphere_radius").exp;

                var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float3];
                //TODOPAUL : overkill way to build a scale matrix
                var radiusScale = new VFXExpressionTRSToMatrix(zero, zero, new VFXExpressionCombine(radius, radius, radius));
                var finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);
                var invFinalTransform = new VFXExpressionTransposeMatrix(new VFXExpressionInverseMatrix(finalTransform));
                yield return new VFXNamedExpression(finalTransform, "transform");
                yield return new VFXNamedExpression(invFinalTransform, "inverseTranspose");
                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, radius, thickness), "volumeFactor");
            }
        }

        public override string source
        {
            get
            {
                var outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = arcSphere_arc * RAND;";
                else
                    outSource += @"float theta = arcSphere_arc * rcSequencer;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);
float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
float3 finalDir = float3(sincosTheta, cosPhi);
float3 finalPos = float3(sincosTheta, cosPhi) * rNorm;
finalPos = mul(transform, float4(finalPos, 1.0f)).xyz;
finalDir = mul(inverseTranspose, float4(finalDir, 0.0f));
finalDir = normalize(finalDir);";

                outSource += string.Format(composeDirectionFormatString, "finalDir") + "\n";
                outSource += string.Format(composePositionFormatString, "finalPos");

                return outSource;
            }
        }
    }
}
