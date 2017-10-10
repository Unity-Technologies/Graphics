using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionSphere : PositionBase
    {
        public override string name { get { return "Position (Sphere)"; } }

        public class InputProperties
        {
            [Tooltip("The sphere used for positioning particles.")]
            public ArcSphere Sphere = new ArcSphere() { radius = 1.0f, arc = Mathf.PI * 2.0f };
        }

        public override string source
        {
            get
            {
                string outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Randomized)
                    outSource += @"float theta = Sphere_arc * RAND;";
                else
                    outSource += @"float theta = Sphere_arc;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);

direction = float3(sincosTheta, cosPhi);
position += direction * (rNorm * Sphere_radius) + Sphere_center;
";

                return outSource;
            }
        }
    }
}
