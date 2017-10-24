using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Collision")]
    class CollisionSphere : CollisionBase
    {
        public override string name { get { return "Collider (Sphere)"; } }

        public class InputProperties
        {
            [Tooltip("The collision sphere.")]
            public Sphere Sphere = new Sphere() { radius = 1.0f };
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - Sphere_center;
float sqrLength = dot(dir, dir);
if (colliderSign * sqrLength <= colliderSign * Sphere_radius * Sphere_radius)
{
    float dist = sqrt(sqrLength);
    float3 n = colliderSign * dir / dist;
";

                Source += collisionResponseSource;
                Source += @"
    position -= n * (dist - Sphere_radius) * colliderSign;
}";
                return Source;
            }
        }
    }
}
