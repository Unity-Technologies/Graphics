using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class PositionCircleDeprecatedV2 : PositionBase
    {
        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.Circle);
            ReplaceModel(newPositionShape, this);
        }


        public override string name { get { return string.Format(base.name, "Arc Circle"); } }

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
