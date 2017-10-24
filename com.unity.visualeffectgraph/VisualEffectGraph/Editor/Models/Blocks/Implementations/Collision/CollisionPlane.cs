using UnityEngine;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Collision")]
    class CollisionPlane : CollisionBase
    {
        public override string name { get { return "Collider (Plane)"; } }

        public class InputProperties
        {
            [Tooltip("The collision plane.")]
            public Plane Plane = new Plane() { normal = Vector3.up };
        }

        public override string source
        {
            get
            {
                string Source = @"
float3 nextPos = position + velocity * deltaTime;
float3 n = Plane_normal * colliderSign;
float w = dot(Plane_position, n);
float distToPlane = dot(nextPos, n) - w;
if (distToPlane < 0.0f)
{
";

                Source += collisionResponseSource;
                Source += @"
    position -= n * distToPlane;
}";
                return Source;
            }
        }
    }
}
