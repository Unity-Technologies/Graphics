using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionCircle : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Circle"); } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the circle used for positioning the particles.")]
            public ArcCircle ArcCircle = ArcCircle.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float ArcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;


        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var volumeFactor = base.parameters.FirstOrDefault(o => o.name == "volumeFactor").exp;
                var arcCircle_arc = base.parameters.FirstOrDefault(o => o.name == "ArcCircle_arc").exp;
                var arcCircleRadius = base.parameters.FirstOrDefault(o => o.name == "ArcCircle_circle_radius").exp;
                var arcSequencer = base.parameters.FirstOrDefault(o => o.name == "ArcSequencer").exp;

                VFXExpression theta = null;
                if (spawnMode == SpawnMode.Random)
                    theta = arcCircle_arc * new VFXExpressionRandom(true, new RandId(this,0));
                else
                    theta = arcCircle_arc * arcSequencer;

                var one = VFXOperatorUtility.OneExpression[UnityEngine.VFX.VFXValueType.Float];

                var rNorm = VFXOperatorUtility.Sqrt(volumeFactor + (one - volumeFactor) * new VFXExpressionRandom(true, new RandId(this, 1))) * arcCircleRadius;
                var sinTheta = new VFXExpressionSin(theta);
                var cosTheta = new VFXExpressionCos(theta);

                yield return new VFXNamedExpression(rNorm, "rNorm");
                yield return new VFXNamedExpression(sinTheta, "sinTheta");
                yield return new VFXNamedExpression(cosTheta, "cosTheta");
                yield return base.parameters.FirstOrDefault(o => o.name == "ArcCircle_circle_center");

                if (compositionPosition == AttributeCompositionMode.Blend)
                    yield return base.parameters.FirstOrDefault(o => o.name == "blendPosition");
                if (compositionDirection == AttributeCompositionMode.Blend)
                    yield return base.parameters.FirstOrDefault(o => o.name == "blendDirection");


            }
        }

        public override string source
        {
            get
            {
                string outSource = string.Format(composeDirectionFormatString, "float3(sinTheta, cosTheta, 0.0f)");
                outSource += VFXBlockUtility.GetComposeString(compositionPosition, "position.xy", "float2(sinTheta, cosTheta) * rNorm + ArcCircle_circle_center.xy", "blendPosition") + "\n";
                outSource += VFXBlockUtility.GetComposeString(compositionPosition, "position.z", " ArcCircle_circle_center.z", "blendPosition");
                return outSource;
            }
        }
    }
}
