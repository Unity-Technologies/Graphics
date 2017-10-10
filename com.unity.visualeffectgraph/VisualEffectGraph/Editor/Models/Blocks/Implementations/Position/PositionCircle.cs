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
            [Tooltip("The circle used for positioning particles.")]
            public ArcCircle Circle = new ArcCircle() { radius = 1.0f, arc = Mathf.PI * 2.0f };
        }

        public override string source
        {
            get
            {
                string outSource = @"";
                if (spawnMode == SpawnMode.Randomized)
                    outSource += @"float theta = Circle_arc * RAND;";
                else
                    outSource += @"float theta = Circle_arc;";

                outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);

direction = float3(sincosTheta, 0.0f);
position.xy += sincosTheta * rNorm * Circle_radius + Circle_center;
";

                return outSource;
            }
        }
    }
}
