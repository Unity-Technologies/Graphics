using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionSphereDeprecated : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Sphere (deprecated)"); } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere used for positioning the particles.")]
            public ArcSphere ArcSphere = ArcSphere.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;

        public override void Sanitize(int version)
        {
            var newPositionSphereV2 = ScriptableObject.CreateInstance<PositionSphereDeprecatedV2>();
            SanitizeHelper.MigrateBlockTShapeFromShape(newPositionSphereV2, this);

            var newPositionSphere = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionSphere, newPositionSphereV2, PositionShapeBase.Type.Sphere);

            ReplaceModel(newPositionSphere, this);
        }

        public override string source
        {
            get
            {
                string outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = ArcSphere_arc * RAND;";
                else
                    outSource += @"float theta = ArcSphere_arc * ArcSequencer;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
float3 sphereNormal = float3(sincosTheta, cosPhi);
";

                outSource += string.Format(composeDirectionFormatString, "sphereNormal");
                outSource += string.Format(composePositionFormatString, "sphereNormal * (rNorm * ArcSphere_sphere_radius) + ArcSphere_sphere_center");

                return outSource;
            }
        }
    }
}
