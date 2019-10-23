using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionCircle : PositionBase
    {
        public override string name { get { return "Position (Circle)"; } }
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

        protected override bool needDirectionWrite
        {
            get
            {
                return true;
            }
        }


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
                    theta = arcCircle_arc * new VFXExpressionRandom(true);
                else
                    theta = arcCircle_arc * arcSequencer;

                var one = VFXOperatorUtility.OneExpression[UnityEngine.VFX.VFXValueType.Float];

                var rNorm = VFXOperatorUtility.Sqrt(volumeFactor + (one - volumeFactor) * new VFXExpressionRandom(true)) * arcCircleRadius;
                var sinTheta = new VFXExpressionSin(theta);
                var cosTheta = new VFXExpressionCos(theta);

                yield return new VFXNamedExpression(rNorm, "rNorm");
                yield return new VFXNamedExpression(sinTheta, "sinTheta");
                yield return new VFXNamedExpression(cosTheta, "cosTheta");
                yield return base.parameters.FirstOrDefault(o => o.name == "ArcCircle_circle_center");
            }
        }

        public override string source
        {
            get
            {
                return @"
direction = float3(sinTheta, cosTheta, 0.0f);
position.xy += float2(sinTheta, cosTheta) * rNorm + ArcCircle_circle_center.xy;
position.z += ArcCircle_circle_center.z;
";
            }
        }
    }
}
