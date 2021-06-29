using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionCircle : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Arc Circle"); } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the circle used for positioning the particles.")]
            public TArcCircle arcCircle = TArcCircle.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float arcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlot = GetExpressionsFromSlots(this);
                var arcCircle_arc = allSlot.FirstOrDefault(o => o.name == "arcCircle_arc").exp;
                var arcCircleRadius = allSlot.FirstOrDefault(o => o.name == "arcCircle_circle_radius").exp;
                var arcSequencer = allSlot.FirstOrDefault(o => o.name == "arcSequencer").exp;

                VFXExpression theta = null;
                if (spawnMode == SpawnMode.Random)
                    theta = arcCircle_arc * new VFXExpressionRandom(true, new RandId(this, 0));
                else
                    theta = arcCircle_arc * arcSequencer;

                var one = VFXOperatorUtility.OneExpression[VFXValueType.Float];

                var thickness = allSlot.FirstOrDefault(o => o.name == nameof(ThicknessProperties.Thickness)).exp;
                var volumeFactor = CalculateVolumeFactor(positionMode, arcCircleRadius, thickness);
                var rNorm = VFXOperatorUtility.Sqrt(volumeFactor + (one - volumeFactor) * new VFXExpressionRandom(true, new RandId(this, 1)));
                var sinTheta = new VFXExpressionSin(theta);
                var cosTheta = new VFXExpressionCos(theta);

                yield return new VFXNamedExpression(rNorm, "rNorm");
                yield return new VFXNamedExpression(sinTheta, "sinTheta");
                yield return new VFXNamedExpression(cosTheta, "cosTheta");

                if (compositionPosition == AttributeCompositionMode.Blend)
                    yield return allSlot.FirstOrDefault(o => o.name == "blendPosition");
                if (compositionDirection == AttributeCompositionMode.Blend)
                    yield return base.parameters.FirstOrDefault(o => o.name == "blendDirection");

                var eulerAngle = allSlot.FirstOrDefault(o => o.name == "arcCircle_circle_angles").exp;
                var center = allSlot.FirstOrDefault(o => o.name == "arcCircle_circle_center").exp;

                var transform = allSlot.FirstOrDefault(o => o.name == "arcCircle_circle_transform").exp;
                var radiusScale = VFXOperatorUtility.UniformScaleMatrix(arcCircleRadius);
                var finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);
                var invFinalTransform = new VFXExpressionTransposeMatrix(new VFXExpressionInverseTRSMatrix(finalTransform));
                yield return new VFXNamedExpression(finalTransform, "transform");
                yield return new VFXNamedExpression(invFinalTransform, "inverseTranspose");
            }
        }

        public override string source
        {
            get
            {
                var outSource = @"
float3 finalDir = float3(sinTheta, cosTheta, 0.0f);
float3 finalPos = float3(sinTheta, cosTheta, 0.0f) * rNorm;
finalPos = mul(transform, float4(finalPos, 1.0f)).xyz;
finalDir = mul(inverseTranspose, float4(finalDir, 0.0f)).xyz;
finalDir = normalize(finalDir);
";
                outSource += string.Format(composeDirectionFormatString, "finalDir") + "\n";
                outSource += string.Format(composePositionFormatString, "finalPos") + "\n";
                return outSource;
            }
        }
    }
}
