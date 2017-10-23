using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Collision")]
    class CollisionCylinder : CollisionBase
    {
        public override string name { get { return "Collider (Cylinder)"; } }

        public class InputProperties
        {
            [Tooltip("The collision cylinder.")]
            public Cylinder Cylinder = new Cylinder() { height = 1.0f, radius = 0.5f };
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - Cylinder_center;
if (abs(dir.y) <= Cylinder_height * 0.5f)
{
    float sqrLength = dot(dir.xz, dir.xz);
    if (sign * sqrLength <= sign * Cylinder_radius * Cylinder_radius)
    {
        float dist = sqrt(sqrLength);
        float3 n = float3(sign * dir.xz / dist, 0.0f).xzy;
";

                Source += collisionResponseSource;
                Source += @"
        position.xz -= n.xz * (dist - Cylinder_radius) * sign;
    }
}";
                return Source;
            }
        }
    }
}
