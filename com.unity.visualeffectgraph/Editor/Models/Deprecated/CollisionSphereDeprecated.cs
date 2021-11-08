using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class CollisionSphereDeprecated : CollisionBase
    {
        public override string name { get { return "Collide with Sphere (deprecated)"; } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere with which particles can collide.")]
            public Sphere Sphere = new Sphere() { radius = 1.0f };
        }

        public override void Sanitize(int version)
        {
            var newCollisionSphere = ScriptableObject.CreateInstance<CollisionSphere>();
            SanitizeHelper.MigrateBlockTShapeFromShape(newCollisionSphere, this);
            ReplaceModel(newCollisionSphere, this);
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 dir = nextPos - Sphere_center;
float sqrLength = dot(dir, dir);
float totalRadius = Sphere_radius + colliderSign * radius;
if (colliderSign * sqrLength <= colliderSign * totalRadius * totalRadius)
{
    float dist = sqrt(sqrLength);
    float3 n = colliderSign * dir / dist;
    position -= n * (dist - totalRadius) * colliderSign;
";

                Source += collisionResponseSource;
                Source += @"
}";
                return Source;
            }
        }
    }
}
