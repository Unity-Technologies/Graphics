using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionSphereDeprecatedV2 : PositionBase
    {
        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.Sphere);
            ReplaceModel(newPositionShape, this);
        }

        public override string name { get { return string.Format(base.name, "Arc Sphere"); } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere used for positioning the particles.")]
            public TArcSphere arcSphere = TArcSphere.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float arcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                var outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = arcSphere_arc * RAND;";
                else
                    outSource += @"float theta = arcSphere_arc * arcSequencer;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);
float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
float3 finalDir = float3(sincosTheta, cosPhi);
float3 finalPos = float3(sincosTheta, cosPhi) * rNorm;
finalPos = mul(transform, float4(finalPos, 1.0f)).xyz;
finalDir = mul(inverseTranspose, float4(finalDir, 0.0f)).xyz;
finalDir = normalize(finalDir);";

                outSource += string.Format(composeDirectionFormatString, "finalDir") + "\n";
                outSource += string.Format(composePositionFormatString, "finalPos");

                return outSource;
            }
        }
    }
}
