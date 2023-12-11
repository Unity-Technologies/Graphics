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
            var newCollisionSphere = ScriptableObject.CreateInstance<CollisionSphereDeprecatedV2>();
            SanitizeHelper.MigrateBlockTShapeFromShape(newCollisionSphere, this);
            var newCollisionSphereShape = ScriptableObject.CreateInstance<CollisionShape>();
            SanitizeHelper.MigrateBlockCollisionShapeToComposed(newCollisionSphereShape, newCollisionSphere, CollisionShapeBase.Type.Sphere);
            ReplaceModel(newCollisionSphereShape, this);
        }
    }
}
